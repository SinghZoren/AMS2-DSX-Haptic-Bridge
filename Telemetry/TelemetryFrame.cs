namespace Ams2DsxBridge.Telemetry;

public readonly struct WheelFrame
{
    public readonly double SuspensionDeflection;
    public readonly double SuspensionVelocity;
    public readonly double SuspForce;
    public readonly double BrakePressure;
    public readonly double TireLoad;
    public readonly double GripFraction;
    public readonly double SlipRatio;
    public readonly double LateralSlipVel;
    public readonly double RotationRad;
    public readonly double AngularVelocity;
    public readonly byte SurfaceType;

    public WheelFrame(double suspDefl, double suspVel, double suspForce, double brakePressure,
        double tireLoad, double gripFract, double slipRatio, double lateralSlipVel,
        double rotationRad, double angularVel, byte surfaceType)
    {
        SuspensionDeflection = suspDefl;
        SuspensionVelocity = suspVel;
        SuspForce = suspForce;
        BrakePressure = brakePressure;
        TireLoad = tireLoad;
        GripFraction = gripFract;
        SlipRatio = slipRatio;
        LateralSlipVel = lateralSlipVel;
        RotationRad = rotationRad;
        AngularVelocity = angularVel;
        SurfaceType = surfaceType;
    }
}

public readonly struct TelemetryFrame
{
    public readonly double DeltaTime;
    public readonly double ElapsedTime;

    public readonly double BrakePedal;
    public readonly double ThrottlePedal;
    public readonly double SteeringInput;

    public readonly double SpeedMps;
    public readonly double SpeedKph;

    public readonly double LateralAccel;
    public readonly double LongitudinalAccel;

    public readonly double YawRate;
    public readonly double YawAccel;

    public readonly double EngineRpm;
    public readonly double EngineMaxRpm;
    public readonly double EngineRpmNormalized;
    public readonly int Gear;

    public readonly double LastImpactET;
    public readonly double LastImpactMagnitude;
    public readonly bool ImpactThisTick;

    public readonly WheelFrame WheelFL;
    public readonly WheelFrame WheelFR;
    public readonly WheelFrame WheelRL;
    public readonly WheelFrame WheelRR;

    public readonly double AvgFrontGrip;
    public readonly double AvgRearGrip;
    public readonly double AvgFrontSlipRatio;
    public readonly double AvgRearSlipRatio;
    public readonly double MaxSuspVelocity;
    public readonly double MaxLeftSuspVelocity;
    public readonly double MaxRightSuspVelocity;
    public readonly bool IsStationary;
    public readonly double OversteerAngle;

    public TelemetryFrame(double deltaTime, double elapsedTime,
        double brake, double throttle, double steering,
        double speedMps,
        double latAccel, double longAccel,
        double yawRate, double yawAccel,
        double engineRpm, double engineMaxRpm, int gear,
        double lastImpactET, double lastImpactMag, bool impactThisTick,
        WheelFrame fl, WheelFrame fr, WheelFrame rl, WheelFrame rr,
        double oversteerAngle,
        double estFrontGrip, double estRearGrip)
    {
        DeltaTime = deltaTime;
        ElapsedTime = elapsedTime;
        BrakePedal = brake;
        ThrottlePedal = throttle;
        SteeringInput = steering;
        SpeedMps = speedMps;
        SpeedKph = speedMps * 3.6;
        LateralAccel = latAccel;
        LongitudinalAccel = longAccel;
        YawRate = yawRate;
        YawAccel = yawAccel;
        EngineRpm = engineRpm;
        EngineMaxRpm = engineMaxRpm;
        EngineRpmNormalized = engineMaxRpm > 0 ? Math.Clamp(engineRpm / engineMaxRpm, 0, 1) : 0;
        Gear = gear;
        LastImpactET = lastImpactET;
        LastImpactMagnitude = lastImpactMag;
        ImpactThisTick = impactThisTick;
        WheelFL = fl;
        WheelFR = fr;
        WheelRL = rl;
        WheelRR = rr;
        AvgFrontGrip = estFrontGrip;
        AvgRearGrip = estRearGrip;
        AvgFrontSlipRatio = (Math.Abs(fl.SlipRatio) + Math.Abs(fr.SlipRatio)) * 0.5;
        AvgRearSlipRatio = (Math.Abs(rl.SlipRatio) + Math.Abs(rr.SlipRatio)) * 0.5;
        MaxSuspVelocity = Math.Max(Math.Max(Math.Abs(fl.SuspensionVelocity), Math.Abs(fr.SuspensionVelocity)),
                                   Math.Max(Math.Abs(rl.SuspensionVelocity), Math.Abs(rr.SuspensionVelocity)));
        MaxLeftSuspVelocity = Math.Max(Math.Abs(fl.SuspensionVelocity), Math.Abs(rl.SuspensionVelocity));
        MaxRightSuspVelocity = Math.Max(Math.Abs(fr.SuspensionVelocity), Math.Abs(rr.SuspensionVelocity));
        IsStationary = speedMps < 0.5;
        OversteerAngle = oversteerAngle;
    }
}

/// <summary>
/// Builds TelemetryFrame from AMS2/pCARS2 shared memory byte buffer.
/// Uses byte offsets from Ams2Offsets to read fields with BitConverter.
/// Estimates grip from ABS/TCS flags and acceleration since AMS2 doesn't
/// expose per-wheel grip fraction or tire load directly.
/// </summary>
public sealed class TelemetryFrameBuilder
{
    private long _prevTickMs;
    private float _prevCollisionMagnitude;
    private uint _prevCrashState;
    private float _prevAeroDamage;
    private float _prevEngineDamage;
    private bool _initialized;

    private readonly double _wheelbaseMeter;
    private readonly double _maxSteerAngleRad;

    public TelemetryFrameBuilder(double wheelbaseMeter = 2.6, double maxSteerAngleDeg = 20.0)
    {
        _wheelbaseMeter = wheelbaseMeter;
        _maxSteerAngleRad = maxSteerAngleDeg * Math.PI / 180.0;
    }

    private static float F(byte[] buf, int offset) => BitConverter.ToSingle(buf, offset);
    private static int I32(byte[] buf, int offset) => BitConverter.ToInt32(buf, offset);
    private static uint U32(byte[] buf, int offset) => BitConverter.ToUInt32(buf, offset);

    public TelemetryFrame Build(byte[] buf)
    {
        // Delta time from wall clock (AMS2 doesn't provide per-frame delta)
        long nowMs = Environment.TickCount64;
        double dt = _initialized ? Math.Max((nowMs - _prevTickMs) / 1000.0, 0.001) : 0.008;
        _prevTickMs = nowMs;

        // Inputs
        float brake = Math.Clamp(F(buf, Ams2Offsets.UnfilteredBrake), 0, 1);
        float throttle = Math.Clamp(F(buf, Ams2Offsets.UnfilteredThrottle), 0, 1);
        float steering = Math.Clamp(F(buf, Ams2Offsets.UnfilteredSteering), -1, 1);

        // Speed (AMS2 provides it directly)
        float speedMps = Math.Max(F(buf, Ams2Offsets.Speed), 0);

        // Engine
        float rpm = F(buf, Ams2Offsets.Rpm);
        float maxRpm = F(buf, Ams2Offsets.MaxRPM);
        int gear = I32(buf, Ams2Offsets.Gear);

        // Acceleration: X=lateral, Z=longitudinal
        float latAccel = F(buf, Ams2Offsets.LocalAcceleration);           // [0] = X
        float longAccel = F(buf, Ams2Offsets.LocalAcceleration + 8);      // [2] = Z

        // Angular velocity: Y=yaw
        float yawRate = F(buf, Ams2Offsets.AngularVelocity + 4);          // [1] = Y

        // Car flags for ABS/TCS state
        uint carFlags = U32(buf, Ams2Offsets.CarFlags);
        bool absActive = (carFlags & (uint)Ams2CarFlags.Abs) != 0;
        bool tcsActive = (carFlags & (uint)Ams2CarFlags.TractionControl) != 0;

        // Impact detection from crash state + collision + damage changes
        uint crashState = U32(buf, Ams2Offsets.CrashState);
        float collisionMag = F(buf, Ams2Offsets.LastOpponentCollisionMagnitude);
        float aeroDamage = F(buf, Ams2Offsets.AeroDamage);
        float engineDamage = F(buf, Ams2Offsets.EngineDamage);

        bool impactThisTick = false;
        float impactMagnitude = 0;
        if (_initialized)
        {
            // Car-to-car collision: magnitude changed
            if (Math.Abs(collisionMag - _prevCollisionMagnitude) > 0.5f && collisionMag > 0.5f)
            {
                impactThisTick = true;
                impactMagnitude = collisionMag;
            }

            // Wall/object hit: crash state transitions to LargeProp
            if (crashState == Ams2CrashState.LargeProp && _prevCrashState != Ams2CrashState.LargeProp)
            {
                impactThisTick = true;
                impactMagnitude = Math.Max(impactMagnitude, 40f);
            }

            // Damage increase indicates impact
            float dmgDelta = Math.Max(
                aeroDamage - _prevAeroDamage,
                engineDamage - _prevEngineDamage);
            if (dmgDelta > 0.01f)
            {
                impactThisTick = true;
                impactMagnitude = Math.Max(impactMagnitude, dmgDelta * 150f);
            }
        }
        _prevCollisionMagnitude = collisionMag;
        _prevCrashState = crashState;
        _prevAeroDamage = aeroDamage;
        _prevEngineDamage = engineDamage;

        // Per-wheel data
        var wheels = new WheelFrame[4];
        for (int i = 0; i < 4; i++)
        {
            float suspTravel = F(buf, Ams2Offsets.SuspensionTravel + i * 4);
            float suspVel = F(buf, Ams2Offsets.SuspensionVelocity + i * 4);
            float tyreRps = F(buf, Ams2Offsets.TyreRPS + i * 4);
            uint terrain = U32(buf, Ams2Offsets.Terrain + i * 4);

            // Slip ratio from tire angular speed vs ground speed
            const double tireRadius = 0.33; // typical racing tire radius in metres
            double wheelSpeed = Math.Abs(tyreRps) * 2.0 * Math.PI * tireRadius;
            double groundSpeed = Math.Max(Math.Abs(speedMps), 0.1);
            double slipRatio = (wheelSpeed - groundSpeed) / groundSpeed;

            double angularVelocity = tyreRps * 2.0 * Math.PI;

            wheels[i] = new WheelFrame(
                suspTravel,         // SuspensionDeflection (using travel as proxy)
                suspVel,            // SuspensionVelocity (provided directly by AMS2)
                0,                  // SuspForce - not available in AMS2
                brake,              // BrakePressure - use global brake input as proxy
                0,                  // TireLoad - not available in AMS2
                0,                  // GripFraction - estimated at frame level below
                slipRatio,
                0,                  // LateralSlipVel - not available in AMS2
                0,                  // RotationRad - not tracked
                angularVelocity,
                (byte)(terrain & 0xFF));
        }

        // Oversteer calculation (kinematic model)
        double oversteerAngle = 0;
        if (speedMps > 3.0)
        {
            double steerAngle = steering * _maxSteerAngleRad;
            double expectedYawRate = (steerAngle * speedMps) / _wheelbaseMeter;
            double yawDiff = Math.Abs(yawRate) - Math.Abs(expectedYawRate);
            if (yawDiff > 0)
                oversteerAngle = yawDiff * (180.0 / Math.PI);
        }

        // Grip estimation using ABS/TCS flags and acceleration
        // The effects engines check grip vs thresholds (ABS: 0.85, TCS: 0.60)
        // When ABS/TCS is active, we set grip below those thresholds
        double totalAccel = Math.Sqrt(latAccel * latAccel + longAccel * longAccel);
        double gripUsed = Math.Clamp(totalAccel / 25.0, 0, 1);

        double estFrontGrip, estRearGrip;
        if (absActive)
        {
            // ABS active: grip below 0.85 threshold, scale with brake intensity
            estFrontGrip = Math.Clamp(0.85 - brake * 0.35, 0.5, 0.82);
        }
        else
        {
            estFrontGrip = brake > 0.1
                ? Math.Clamp(1.0 - gripUsed * 0.15, 0.85, 1.0)
                : Math.Clamp(1.0 - gripUsed * 0.1, 0.9, 1.0);
        }

        if (tcsActive)
        {
            // TCS active: grip below 0.60 threshold, scale with throttle
            estRearGrip = Math.Clamp(0.60 - throttle * 0.25, 0.35, 0.55);
        }
        else
        {
            estRearGrip = throttle > 0.3
                ? Math.Clamp(1.0 - gripUsed * 0.15, 0.60, 1.0)
                : Math.Clamp(1.0 - gripUsed * 0.1, 0.7, 1.0);
        }

        _initialized = true;

        double elapsedTime = nowMs / 1000.0;
        return new TelemetryFrame(dt, elapsedTime,
            brake, throttle, steering,
            speedMps,
            latAccel, longAccel,
            yawRate, 0, // yaw acceleration not available in AMS2
            rpm, maxRpm, gear,
            elapsedTime, impactMagnitude, impactThisTick,
            wheels[0], wheels[1], wheels[2], wheels[3],
            oversteerAngle, estFrontGrip, estRearGrip);
    }

    public void Reset()
    {
        _initialized = false;
        _prevTickMs = 0;
        _prevCollisionMagnitude = 0;
        _prevCrashState = 0;
        _prevAeroDamage = 0;
        _prevEngineDamage = 0;
    }
}
