using Rf2DsxBridge.Config;
using Rf2DsxBridge.Telemetry;

namespace Rf2DsxBridge.Effects;

public sealed class TriggerEffectsEngine
{
    private readonly AppConfig _config;
    private double _impactDecay;

    public TriggerEffect LeftTrigger { get; private set; }
    public TriggerEffect RightTrigger { get; private set; }

    public TriggerEffectsEngine(AppConfig config)
    {
        _config = config;
    }

    public void Update(in TelemetryFrame frame)
    {
        if (frame.ImpactThisTick)
            _impactDecay = Math.Clamp(frame.LastImpactMagnitude * _config.ImpactTriggerGain / 50.0, 0.3, 1.0);
        else
            _impactDecay = Math.Max(0, _impactDecay - frame.DeltaTime * 5.0);

        LeftTrigger = ComputeBrakeTrigger(in frame);
        RightTrigger = ComputeThrottleTrigger(in frame);
    }

    private TriggerEffect ComputeBrakeTrigger(in TelemetryFrame frame)
    {
        if (frame.BrakePedal < _config.BrakeDeadzone)
            return TriggerEffect.Off();

        if (_impactDecay > 0.2)
        {
            int amp = Math.Clamp((int)(8 * _impactDecay), 1, 8);
            return TriggerEffect.Vibrate(_config.BrakeStartPos, amp, 60);
        }

        bool absActive = frame.BrakePedal > 0.15 && frame.AvgFrontGrip < _config.AbsGripThreshold && !frame.IsStationary;
        if (absActive)
        {
            double severity = Math.Clamp((1.0 - frame.AvgFrontGrip) / (1.0 - _config.AbsGripThreshold), 0, 1);
            int amp = Math.Clamp((int)(8 * severity * _config.AbsTriggerGain), 1, 8);
            return TriggerEffect.Vibrate(_config.BrakeStartPos, amp, (byte)Math.Clamp(_config.AbsTriggerFreqHz, 10, 120));
        }

        int strength = Math.Clamp((int)Math.Round(frame.BrakePedal * _config.MaxBrakeStrength), 1, 8);
        return TriggerEffect.Resistance(_config.BrakeStartPos, strength);
    }

    private TriggerEffect ComputeThrottleTrigger(in TelemetryFrame frame)
    {
        if (frame.ThrottlePedal < _config.ThrottleDeadzone)
            return TriggerEffect.Off();

        if (_impactDecay > 0.2)
        {
            int amp = Math.Clamp((int)(6 * _impactDecay), 1, 8);
            return TriggerEffect.Vibrate(_config.ThrottleStartPos, amp, 50);
        }

        bool tcActive = frame.ThrottlePedal > 0.2 && frame.AvgRearGrip < _config.TcGripThreshold && !frame.IsStationary;
        if (tcActive)
        {
            double severity = Math.Clamp((1.0 - frame.AvgRearGrip) / (1.0 - _config.TcGripThreshold), 0, 1);
            int amp = Math.Clamp((int)(8 * severity * _config.TcTriggerGain), 1, 8);
            return TriggerEffect.Vibrate(_config.ThrottleStartPos, amp, (byte)Math.Clamp(_config.TcTriggerFreqHz, 10, 120));
        }

        if (frame.OversteerAngle > _config.OversteerThresholdDeg && !frame.IsStationary)
        {
            double severity = Math.Clamp((frame.OversteerAngle - _config.OversteerThresholdDeg) / 20.0, 0, 1);
            int amp = Math.Clamp((int)(6 * severity * _config.OversteerTriggerGain), 1, 8);
            return TriggerEffect.Vibrate(_config.ThrottleStartPos, amp, 25);
        }

        int strength = Math.Clamp((int)Math.Round(frame.ThrottlePedal * _config.MaxThrottleStrength), 1, 8);
        return TriggerEffect.Resistance(_config.ThrottleStartPos, strength);
    }

    public void Reset()
    {
        LeftTrigger = TriggerEffect.Off();
        RightTrigger = TriggerEffect.Off();
        _impactDecay = 0;
    }
}
