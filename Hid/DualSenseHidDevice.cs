using HidSharp;

namespace Ams2DsxBridge.Hid;

public sealed class DualSenseHidDevice : IDisposable
{
    private const int VendorId = 0x054C;
    private static readonly int[] ProductIds = [0x0CE6, 0x0DF2];

    private HidDevice? _device;
    private HidStream? _stream;
    private DualSenseReport? _report;
    private bool _connected;
    private readonly object _lock = new();

    public bool IsConnected => _connected;

    public DualSenseHidDevice()
    {
        DeviceList.Local.Changed += OnDeviceListChanged;
    }

    public bool TryConnect()
    {
        lock (_lock)
        {
            if (_connected) return true;

            foreach (int pid in ProductIds)
            {
                var devices = DeviceList.Local.GetHidDevices(VendorId, pid);
                foreach (var dev in devices)
                {
                    try
                    {
                        int maxOut = dev.GetMaxOutputReportLength();
                        if (maxOut < 48) continue;

                        if (dev.TryOpen(out var stream))
                        {
                            _device = dev;
                            _stream = stream;
                            _report = new DualSenseReport(maxOut);
                            _connected = true;
                            string name = pid == 0x0DF2 ? "DualSense Edge" : "DualSense";
                            Console.WriteLine($"[HID] Connected to {name} (report size={maxOut})");
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HID] Failed to open device: {ex.Message}");
                    }
                }
            }
            return false;
        }
    }

    public DualSenseReport? BeginReport()
    {
        if (!_connected || _report == null) return null;
        _report.Clear();
        return _report;
    }

    public bool SendReport()
    {
        if (!_connected || _stream == null || _report == null) return false;

        try
        {
            _stream.Write(_report.Buffer);
            return true;
        }
        catch (IOException)
        {
            Console.WriteLine("[HID] Write failed - device disconnected.");
            Disconnect();
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HID] Write error: {ex.Message}");
            Disconnect();
            return false;
        }
    }

    public void SendSafeState()
    {
        var report = BeginReport();
        if (report != null)
        {
            report.SetRumble(0, 0);
            report.SetLeftTriggerOff();
            report.SetRightTriggerOff();
            SendReport();
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            _connected = false;
            _stream?.Dispose();
            _stream = null;
            _device = null;
            _report = null;
        }
    }

    private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e)
    {
        if (!_connected)
        {
            TryConnect();
        }
    }

    public void Dispose()
    {
        DeviceList.Local.Changed -= OnDeviceListChanged;
        try { SendSafeState(); } catch { }
        Disconnect();
    }
}
