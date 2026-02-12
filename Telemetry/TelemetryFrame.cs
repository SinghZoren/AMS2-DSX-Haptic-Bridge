namespace Rf2DsxBridge.Telemetry;

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
        double oversteerAngle)
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
        AvgFrontGrip = (fl.GripFraction + fr.GripFraction) * 0.5;
        AvgRearGrip = (rl.GripFraction + rr.GripFraction) * 0.5;
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

public sealed class TelemetryFrameBuilder
{
    private double[] _prevSuspDeflection = new double[4];
    private double[] _prevWheelRotation = new double[4];
    private double _prevImpactET;
    private bool _initialized;

    private readonly double _wheelbaseMeter;
    private readonly double _maxSteerAngleRad;

    public TelemetryFrameBuilder(double wheelbaseMeter = 2.6, double maxSteerAngleDeg = 20.0)
    {
        _wheelbaseMeter = wheelbaseMeter;
        _maxSteerAngleRad = maxSteerAngleDeg * Math.PI / 180.0;
    }

    public TelemetryFrame Build(in rF2VehicleTelemetry veh)
    {
        double dt = Math.Max(veh.mDeltaTime, 0.001);

        double brake = Math.Clamp(veh.mUnfilteredBrake, 0, 1);
        double throttle = Math.Clamp(veh.mUnfilteredThrottle, 0, 1);
        double steering = Math.Clamp(veh.mUnfilteredSteering, -1, 1);

        double speedMps = Math.Sqrt(veh.mLocalVel.x * veh.mLocalVel.x +
                                     veh.mLocalVel.y * veh.mLocalVel.y +
                                     veh.mLocalVel.z * veh.mLocalVel.z);

        double latAccel = veh.mLocalAccel.x;
        double longAccel = veh.mLocalAccel.z;
        double yawRate = veh.mLocalRot.y;
        double yawAccel = veh.mLocalRotAccel.y;

        bool impactThisTick = false;
        if (_initialized && veh.mLastImpactET != _prevImpactET && veh.mLastImpactMagnitude > 0.5)
        {
            impactThisTick = true;
        }
        _prevImpactET = veh.mLastImpactET;

        var wheels = new WheelFrame[4];
        for (int i = 0; i < 4; i++)
        {
            ref readonly var w = ref veh.mWheels[i];

            double suspVel = 0;
            double angVel = 0;
            if (_initialized)
            {
                suspVel = (w.mSuspensionDeflection - _prevSuspDeflection[i]) / dt;
                double rotDelta = w.mRotation - _prevWheelRotation[i];
                angVel = rotDelta / dt;
            }
            _prevSuspDeflection[i] = w.mSuspensionDeflection;
            _prevWheelRotation[i] = w.mRotation;

            double slipRatio = w.mLongitudinalPatchVel - w.mLongitudinalGroundVel;
            double lateralSlipVel = w.mLateralPatchVel - w.mLateralGroundVel;

            wheels[i] = new WheelFrame(
                w.mSuspensionDeflection, suspVel, w.mSuspForce, w.mBrakePressure,
                w.mTireLoad, w.mGripFract, slipRatio, lateralSlipVel,
                w.mRotation, angVel, w.mSurfaceType);
        }

        double oversteerAngle = 0;
        if (speedMps > 3.0)
        {
            double steerAngle = steering * _maxSteerAngleRad;
            double expectedYawRate = (steerAngle * speedMps) / _wheelbaseMeter;
            double yawDiff = Math.Abs(yawRate) - Math.Abs(expectedYawRate);
            if (yawDiff > 0)
            {
                oversteerAngle = yawDiff * (180.0 / Math.PI);
            }
        }

        _initialized = true;

        return new TelemetryFrame(dt, veh.mElapsedTime,
            brake, throttle, steering,
            speedMps,
            latAccel, longAccel,
            yawRate, yawAccel,
            veh.mEngineRPM, veh.mEngineMaxRPM, veh.mGear,
            veh.mLastImpactET, veh.mLastImpactMagnitude, impactThisTick,
            wheels[0], wheels[1], wheels[2], wheels[3],
            oversteerAngle);
    }

    public void Reset()
    {
        _initialized = false;
        Array.Clear(_prevSuspDeflection);
        Array.Clear(_prevWheelRotation);
        _prevImpactET = 0;
    }
}
