using Ams2DsxBridge.Dsx;
using Ams2DsxBridge.Effects;

namespace Ams2DsxBridge.Output;

public sealed class DsxOutputSink : IOutputSink
{
    private readonly DsxFileWriter _writer;

    public bool IsConnected => true;

    public DsxOutputSink(string filePath)
    {
        _writer = new DsxFileWriter(filePath);
    }

    public bool TryConnect() => true;

    public void Send(TriggerEffect left, TriggerEffect right, RumbleEffect rumble)
    {
        var output = new TriggerOutput
        {
            LeftMode = MapMode(left),
            LeftForceA = left.StartPosition,
            LeftForceB = left.Mode == TriggerMode.Feedback ? left.Strength : left.Amplitude,
            RightMode = MapMode(right),
            RightForceA = right.StartPosition,
            RightForceB = right.Mode == TriggerMode.Feedback ? right.Strength : right.Amplitude,
        };
        _writer.Write(output);
    }

    private static string MapMode(TriggerEffect e) => e.Mode switch
    {
        TriggerMode.Off => "Normal",
        TriggerMode.Feedback => "Resistance",
        TriggerMode.Vibration => "Resistance",
        _ => "Normal"
    };

    public void SendSafeState() => _writer.WriteNormal();

    public void Dispose() { }
}
