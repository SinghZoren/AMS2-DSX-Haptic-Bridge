using Ams2DsxBridge.Effects;

namespace Ams2DsxBridge.Output;

public interface IOutputSink : IDisposable
{
    bool IsConnected { get; }
    bool TryConnect();
    void Send(TriggerEffect leftTrigger, TriggerEffect rightTrigger, RumbleEffect rumble);
    void SendSafeState();
}
