namespace Rf2DsxBridge.Effects;

public enum TriggerMode : byte
{
    Off,
    Feedback,
    Vibration
}

public struct TriggerEffect
{
    public TriggerMode Mode;
    public int StartPosition;
    public int Strength;
    public int Amplitude;
    public byte FrequencyHz;

    public static TriggerEffect Off() => new() { Mode = TriggerMode.Off };

    public static TriggerEffect Resistance(int startPos, int strength)
        => new() { Mode = TriggerMode.Feedback, StartPosition = startPos, Strength = strength };

    public static TriggerEffect Vibrate(int startPos, int amplitude, byte freqHz)
        => new() { Mode = TriggerMode.Vibration, StartPosition = startPos, Amplitude = amplitude, FrequencyHz = freqHz };
}
