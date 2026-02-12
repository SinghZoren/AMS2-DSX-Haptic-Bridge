namespace Rf2DsxBridge.Effects;

public struct RumbleEffect
{
    public float MotorRight;
    public float MotorLeft;

    public static RumbleEffect None => new() { MotorRight = 0, MotorLeft = 0 };

    public static RumbleEffect operator +(RumbleEffect a, RumbleEffect b)
        => new() { MotorRight = a.MotorRight + b.MotorRight, MotorLeft = a.MotorLeft + b.MotorLeft };

    public static RumbleEffect operator *(RumbleEffect r, float scale)
        => new() { MotorRight = r.MotorRight * scale, MotorLeft = r.MotorLeft * scale };

    public void Clamp()
    {
        MotorRight = Math.Clamp(MotorRight, 0f, 1f);
        MotorLeft = Math.Clamp(MotorLeft, 0f, 1f);
    }

    public byte MotorRightByte => (byte)(Math.Clamp(MotorRight, 0f, 1f) * 255);
    public byte MotorLeftByte => (byte)(Math.Clamp(MotorLeft, 0f, 1f) * 255);
}
