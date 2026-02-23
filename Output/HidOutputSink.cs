using Ams2DsxBridge.Effects;
using Ams2DsxBridge.Hid;

namespace Ams2DsxBridge.Output;

public sealed class HidOutputSink : IOutputSink
{
    private readonly DualSenseHidDevice _device;

    public bool IsConnected => _device.IsConnected;

    public HidOutputSink()
    {
        _device = new DualSenseHidDevice();
    }

    public bool TryConnect() => _device.TryConnect();

    public void Send(TriggerEffect left, TriggerEffect right, RumbleEffect rumble)
    {
        var report = _device.BeginReport();
        if (report == null) return;

        report.SetRumble(rumble.MotorRightByte, rumble.MotorLeftByte);
        ApplyTrigger(report, left, isLeft: true);
        ApplyTrigger(report, right, isLeft: false);
        _device.SendReport();
    }

    private static void ApplyTrigger(DualSenseReport report, TriggerEffect effect, bool isLeft)
    {
        switch (effect.Mode)
        {
            case TriggerMode.Off:
                if (isLeft) report.SetLeftTriggerOff();
                else report.SetRightTriggerOff();
                break;

            case TriggerMode.Feedback:
                if (isLeft) report.SetLeftTriggerFeedback(effect.StartPosition, effect.Strength);
                else report.SetRightTriggerFeedback(effect.StartPosition, effect.Strength);
                break;

            case TriggerMode.Vibration:
                if (isLeft) report.SetLeftTriggerVibration(effect.StartPosition, effect.Amplitude, effect.FrequencyHz);
                else report.SetRightTriggerVibration(effect.StartPosition, effect.Amplitude, effect.FrequencyHz);
                break;
        }
    }

    public void SendSafeState() => _device.SendSafeState();

    public void Dispose() => _device.Dispose();
}
