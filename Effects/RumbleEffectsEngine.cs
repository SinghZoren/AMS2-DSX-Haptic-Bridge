using Rf2DsxBridge.Config;
using Rf2DsxBridge.Telemetry;

namespace Rf2DsxBridge.Effects;

public sealed class RumbleEffectsEngine
{
    private readonly AppConfig _config;

    private double _impactDecay;
    private int _absPhase;
    private int _tcPhase;

    private double _suspAvgSlow;
    private bool _suspAvgInit;
    private double _curbDecay;
    private float _curbSideBiasLeft;

    public RumbleEffect Output { get; private set; }

    public RumbleEffectsEngine(AppConfig config)
    {
        _config = config;
    }

    public void Update(in TelemetryFrame frame)
    {
        UpdateCurbBaseline(in frame);

        var curb = ComputeCurb(in frame);
        bool curbActive = _curbDecay > 0.05;

        var road = ComputeRoadSurface(in frame, curbActive);
        var abs = ComputeAbs(in frame);
        var tc = ComputeTc(in frame);
        var engine = ComputeEngine(in frame);
        var impact = ComputeImpact(in frame);
        var spin = ComputeOversteer(in frame);

        var combined = road * _config.RoadRumbleGain
                     + curb * _config.CurbRumbleGain
                     + abs * _config.AbsRumbleGain
                     + tc * _config.TcRumbleGain
                     + engine * _config.EngineRumbleGain
                     + impact * _config.ImpactRumbleGain
                     + spin * _config.SpinRumbleGain;

        combined = combined * _config.MasterRumbleGain;

        combined.MotorRight = SoftClip(combined.MotorRight);
        combined.MotorLeft = SoftClip(combined.MotorLeft);
        combined.Clamp();

        Output = combined;
    }

    private void UpdateCurbBaseline(in TelemetryFrame frame)
    {
        double avgSusp = (Math.Abs(frame.WheelFL.SuspensionVelocity) + Math.Abs(frame.WheelFR.SuspensionVelocity)
                        + Math.Abs(frame.WheelRL.SuspensionVelocity) + Math.Abs(frame.WheelRR.SuspensionVelocity)) / 4.0;

        if (!_suspAvgInit)
        {
            _suspAvgSlow = avgSusp;
            _suspAvgInit = true;
        }
        else
        {
            const double alpha = 0.005;
            _suspAvgSlow = _suspAvgSlow * (1.0 - alpha) + avgSusp * alpha;
        }
    }

    private RumbleEffect ComputeRoadSurface(in TelemetryFrame frame, bool curbActive)
    {
        if (frame.IsStationary) return RumbleEffect.None;

        float speedFactor = (float)Math.Clamp((frame.SpeedKph - 20.0) / 60.0, 0, 1);

        double avgSusp = (Math.Abs(frame.WheelFL.SuspensionVelocity)
                        + Math.Abs(frame.WheelFR.SuspensionVelocity)
                        + Math.Abs(frame.WheelRL.SuspensionVelocity)
                        + Math.Abs(frame.WheelRR.SuspensionVelocity)) / 4.0;

        float intensity = (float)Math.Clamp(avgSusp * 0.15, 0, 0.3) * speedFactor;

        float roadSuppression = curbActive ? 0.2f : 1.0f;
        intensity *= roadSuppression;

        return new RumbleEffect
        {
            MotorRight = intensity,
            MotorLeft = intensity * 0.1f
        };
    }

    private RumbleEffect ComputeCurb(in TelemetryFrame frame)
    {
        if (frame.IsStationary) { _curbDecay = 0; return RumbleEffect.None; }

        double maxSuspVel = frame.MaxSuspVelocity;
        double baseline = Math.Max(_suspAvgSlow, 0.01);

        double spikeRatio = maxSuspVel / baseline;

        if (spikeRatio > 2.5 && maxSuspVel > _config.CurbSuspVelocityThreshold)
        {
            double hitIntensity = Math.Clamp(
                (maxSuspVel - _config.CurbSuspVelocityThreshold) * _config.CurbRumbleScale, 0.4, 1.0);
            _curbDecay = Math.Max(_curbDecay, hitIntensity);

            _curbSideBiasLeft = frame.MaxLeftSuspVelocity > frame.MaxRightSuspVelocity ? 1.0f : 0.3f;
        }

        if (_curbDecay > 0)
            _curbDecay = Math.Max(0, _curbDecay - frame.DeltaTime * 6.5);

        if (_curbDecay <= 0.02) return RumbleEffect.None;

        float intensity = (float)_curbDecay;
        float rightBias = _curbSideBiasLeft < 0.5f ? 1.0f : 0.3f;

        return new RumbleEffect
        {
            MotorRight = intensity * 0.3f * rightBias,
            MotorLeft = intensity * _curbSideBiasLeft
        };
    }

    private RumbleEffect ComputeAbs(in TelemetryFrame frame)
    {
        bool absActive = frame.BrakePedal > 0.15 && frame.AvgFrontGrip < _config.AbsGripThreshold && !frame.IsStationary;
        if (!absActive) { _absPhase = 0; return RumbleEffect.None; }

        _absPhase++;
        bool on = (_absPhase % 3) < 2;
        double severity = Math.Clamp((1.0 - frame.AvgFrontGrip) / (1.0 - _config.AbsGripThreshold), 0, 1);
        float amp = on ? (float)Math.Clamp(severity * 0.8, 0.15, 0.8) : 0f;

        return new RumbleEffect
        {
            MotorRight = amp,
            MotorLeft = amp * 0.3f
        };
    }

    private RumbleEffect ComputeTc(in TelemetryFrame frame)
    {
        bool tcActive = frame.ThrottlePedal > 0.2 && frame.AvgRearGrip < _config.TcGripThreshold && !frame.IsStationary;
        if (!tcActive) { _tcPhase = 0; return RumbleEffect.None; }

        _tcPhase++;
        bool on = (_tcPhase % 4) < 2;
        double severity = Math.Clamp((1.0 - frame.AvgRearGrip) / (1.0 - _config.TcGripThreshold), 0, 1);
        float amp = on ? (float)Math.Clamp(severity * 0.7, 0.15, 0.7) : 0f;

        return new RumbleEffect
        {
            MotorRight = amp * 0.5f,
            MotorLeft = amp * 0.4f
        };
    }

    private RumbleEffect ComputeEngine(in TelemetryFrame frame)
    {
        if (frame.EngineRpm < 500) return RumbleEffect.None;

        float normalized = (float)frame.EngineRpmNormalized;
        float intensity = normalized * normalized;

        return new RumbleEffect
        {
            MotorRight = intensity * 0.08f,
            MotorLeft = intensity * 0.2f
        };
    }

    private RumbleEffect ComputeImpact(in TelemetryFrame frame)
    {
        if (frame.ImpactThisTick)
            _impactDecay = Math.Clamp(frame.LastImpactMagnitude / 50.0, 0.3, 1.0);
        else
            _impactDecay = Math.Max(0, _impactDecay - frame.DeltaTime * 4.0);

        if (_impactDecay <= 0) return RumbleEffect.None;

        return new RumbleEffect
        {
            MotorRight = (float)_impactDecay * 0.5f,
            MotorLeft = (float)_impactDecay
        };
    }

    private RumbleEffect ComputeOversteer(in TelemetryFrame frame)
    {
        if (frame.OversteerAngle < _config.OversteerThresholdDeg || frame.IsStationary)
            return RumbleEffect.None;

        double severity = Math.Clamp(
            (frame.OversteerAngle - _config.OversteerThresholdDeg) / 30.0, 0, 1);

        return new RumbleEffect
        {
            MotorRight = (float)severity * 0.5f,
            MotorLeft = (float)severity * 0.7f
        };
    }

    private static float SoftClip(float x)
    {
        if (x <= 0f) return 0f;
        if (x <= 0.7f) return x;
        return 0.7f + 0.3f * MathF.Tanh((x - 0.7f) / 0.3f);
    }

    public void Reset()
    {
        Output = RumbleEffect.None;
        _impactDecay = 0;
        _absPhase = 0;
        _tcPhase = 0;
        _curbDecay = 0;
        _suspAvgSlow = 0;
        _suspAvgInit = false;
        _curbSideBiasLeft = 0;
    }
}
