using System.IO.MemoryMappedFiles;

namespace Ams2DsxBridge.Telemetry;

public sealed class TelemetryReader : IDisposable
{
    private MemoryMappedFile? _mmf;
    private MemoryMappedViewAccessor? _accessor;
    private bool _connected;
    private readonly TelemetryFrameBuilder _frameBuilder;
    private byte[] _buffer;
    private int _bufferSize;

    public bool IsConnected => _connected;
    public double BrakePedal { get; private set; }
    public double ThrottlePedal { get; private set; }
    public TelemetryFrame CurrentFrame { get; private set; }

    public TelemetryReader(double wheelbaseMeter = 2.6, double maxSteerAngleDeg = 20.0)
    {
        _buffer = new byte[Ams2Constants.BUFFER_SIZE];
        _bufferSize = Ams2Constants.BUFFER_SIZE;
        _frameBuilder = new TelemetryFrameBuilder(wheelbaseMeter, maxSteerAngleDeg);
    }

    public bool TryConnect()
    {
        if (_connected)
            return true;

        try
        {
            _mmf = MemoryMappedFile.OpenExisting(Ams2Constants.MM_SHARED_MEMORY_NAME);
            _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            long capacity = _accessor.Capacity;
            if (capacity < 7400) // minimum to cover essential fields up to SuspensionVelocity
            {
                Console.WriteLine($"[TelemetryReader] Shared memory too small ({capacity} bytes).");
                Disconnect();
                return false;
            }

            _bufferSize = (int)Math.Min(capacity, Ams2Constants.BUFFER_SIZE);
            _buffer = new byte[_bufferSize];
            _connected = true;
            return true;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TelemetryReader] Error connecting: {ex.Message}");
            return false;
        }
    }

    public bool Update()
    {
        if (!_connected || _accessor == null)
        {
            BrakePedal = 0;
            ThrottlePedal = 0;
            return false;
        }

        try
        {
            // Sequence number validation for data consistency
            // Odd sequence = game is mid-write, data may be inconsistent
            uint seq1, seq2;
            int retries = 0;
            do
            {
                seq1 = _accessor.ReadUInt32(Ams2Offsets.SequenceNumber);
                _accessor.ReadArray(0, _buffer, 0, _bufferSize);
                seq2 = _accessor.ReadUInt32(Ams2Offsets.SequenceNumber);
                retries++;
            } while ((seq1 != seq2 || (seq1 & 1) != 0) && retries < 10);

            if (retries >= 10)
                return false;

            // Check game state - only process when in-game
            uint gameState = BitConverter.ToUInt32(_buffer, Ams2Offsets.GameState);
            if (gameState < Ams2GameState.InGamePlaying || gameState > Ams2GameState.InGameReplay)
            {
                BrakePedal = 0;
                ThrottlePedal = 0;
                return false;
            }

            CurrentFrame = _frameBuilder.Build(_buffer);
            BrakePedal = CurrentFrame.BrakePedal;
            ThrottlePedal = CurrentFrame.ThrottlePedal;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TelemetryReader] Error reading: {ex.Message}");
            Disconnect();
            return false;
        }
    }

    public void Disconnect()
    {
        _connected = false;
        _accessor?.Dispose();
        _accessor = null;
        _mmf?.Dispose();
        _mmf = null;
        BrakePedal = 0;
        ThrottlePedal = 0;
        _frameBuilder.Reset();
        CurrentFrame = default;
    }

    public void Dispose()
    {
        Disconnect();
    }
}
