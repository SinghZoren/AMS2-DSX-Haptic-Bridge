namespace Rf2DsxBridge.Hid;

public sealed class DualSenseReport
{
    public const byte ReportId = 0x02;
    public const byte ValidFlag_CompatibleVibration = 0x01;
    public const byte ValidFlag_HapticsSelect = 0x02;
    public const byte ValidFlag_R2Trigger = 0x04;
    public const byte ValidFlag_L2Trigger = 0x08;
    public const byte ValidFlag_All = ValidFlag_CompatibleVibration | ValidFlag_HapticsSelect
                                    | ValidFlag_R2Trigger | ValidFlag_L2Trigger;

    public const byte TriggerMode_Off = 0x05;
    public const byte TriggerMode_Feedback = 0x21;
    public const byte TriggerMode_Vibration = 0x26;

    private const int Off_ValidFlag0 = 1;
    private const int Off_MotorRight = 3;
    private const int Off_MotorLeft = 4;
    private const int Off_R2Mode = 11;
    private const int Off_R2Params = 12;
    private const int Off_L2Mode = 22;
    private const int Off_L2Params = 23;

    private readonly byte[] _buffer;

    public byte[] Buffer => _buffer;

    public DualSenseReport(int maxReportLength)
    {
        _buffer = new byte[maxReportLength];
    }

    public void Clear()
    {
        Array.Clear(_buffer);
        _buffer[0] = ReportId;
        _buffer[Off_ValidFlag0] = ValidFlag_All;
    }

    public void SetRumble(byte motorRight, byte motorLeft)
    {
        _buffer[Off_MotorRight] = motorRight;
        _buffer[Off_MotorLeft] = motorLeft;
    }

    public void SetRightTriggerOff()
    {
        _buffer[Off_R2Mode] = TriggerMode_Off;
        Array.Clear(_buffer, Off_R2Params, 10);
    }

    public void SetRightTriggerFeedback(int startPosition, int strength)
    {
        _buffer[Off_R2Mode] = TriggerMode_Feedback;
        EncodeFeedbackParams(_buffer.AsSpan(Off_R2Params, 10), startPosition, strength);
    }

    public void SetRightTriggerVibration(int startPosition, int amplitude, byte frequencyHz)
    {
        _buffer[Off_R2Mode] = TriggerMode_Vibration;
        EncodeVibrationParams(_buffer.AsSpan(Off_R2Params, 10), startPosition, amplitude, frequencyHz);
    }

    public void SetLeftTriggerOff()
    {
        _buffer[Off_L2Mode] = TriggerMode_Off;
        Array.Clear(_buffer, Off_L2Params, 10);
    }

    public void SetLeftTriggerFeedback(int startPosition, int strength)
    {
        _buffer[Off_L2Mode] = TriggerMode_Feedback;
        EncodeFeedbackParams(_buffer.AsSpan(Off_L2Params, 10), startPosition, strength);
    }

    public void SetLeftTriggerVibration(int startPosition, int amplitude, byte frequencyHz)
    {
        _buffer[Off_L2Mode] = TriggerMode_Vibration;
        EncodeVibrationParams(_buffer.AsSpan(Off_L2Params, 10), startPosition, amplitude, frequencyHz);
    }

    private static void EncodeFeedbackParams(Span<byte> param, int startPos, int strength)
    {
        startPos = Math.Clamp(startPos, 0, 9);
        strength = Math.Clamp(strength, 0, 8);
        param.Clear();

        if (strength <= 0) return;

        ushort activeZones = 0;
        uint forceZones = 0;
        uint forceBits = (uint)((strength - 1) & 0x07);

        for (int i = startPos; i < 10; i++)
        {
            activeZones |= (ushort)(1 << i);
            forceZones |= forceBits << (3 * i);
        }

        param[0] = (byte)(activeZones & 0xFF);
        param[1] = (byte)((activeZones >> 8) & 0xFF);
        param[2] = (byte)(forceZones & 0xFF);
        param[3] = (byte)((forceZones >> 8) & 0xFF);
        param[4] = (byte)((forceZones >> 16) & 0xFF);
        param[5] = (byte)((forceZones >> 24) & 0xFF);
    }

    private static void EncodeVibrationParams(Span<byte> param, int startPos, int amplitude, byte freqHz)
    {
        startPos = Math.Clamp(startPos, 0, 9);
        amplitude = Math.Clamp(amplitude, 0, 8);
        param.Clear();

        if (amplitude <= 0) return;

        ushort activeZones = 0;
        uint amplitudeZones = 0;
        uint ampBits = (uint)((amplitude - 1) & 0x07);

        for (int i = startPos; i < 10; i++)
        {
            activeZones |= (ushort)(1 << i);
            amplitudeZones |= ampBits << (3 * i);
        }

        param[0] = (byte)(activeZones & 0xFF);
        param[1] = (byte)((activeZones >> 8) & 0xFF);
        param[2] = (byte)(amplitudeZones & 0xFF);
        param[3] = (byte)((amplitudeZones >> 8) & 0xFF);
        param[4] = (byte)((amplitudeZones >> 16) & 0xFF);
        param[5] = (byte)((amplitudeZones >> 24) & 0xFF);
        param[8] = freqHz;
    }
}
