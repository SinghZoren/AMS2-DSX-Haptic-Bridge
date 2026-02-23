using Ams2DsxBridge.Config;

namespace Ams2DsxBridge.Dsx;

public sealed class TriggerMapper
{
    private readonly AppConfig _config;
    private double _brakeSmoothed;
    private double _throttleSmoothed;
    private bool _initialized;

    public double BrakeSmoothed => _brakeSmoothed;
    public double ThrottleSmoothed => _throttleSmoothed;

    public TriggerMapper(AppConfig config)
    {
        _config = config;
    }

    public TriggerOutput Update(double brakeRaw, double throttleRaw)
    {
        double alpha = _config.SmoothingAlpha;
        if (!_initialized)
        {
            _brakeSmoothed = brakeRaw;
            _throttleSmoothed = throttleRaw;
            _initialized = true;
        }
        else
        {
            _brakeSmoothed = alpha * brakeRaw + (1.0 - alpha) * _brakeSmoothed;
            _throttleSmoothed = alpha * throttleRaw + (1.0 - alpha) * _throttleSmoothed;
        }

        var output = new TriggerOutput();

        if (_brakeSmoothed <= _config.BrakeDeadzone)
        {
            output.LeftMode = "Normal";
            output.LeftForceA = 0;
            output.LeftForceB = 0;
        }
        else
        {
            output.LeftMode = "Resistance";
            output.LeftForceA = _config.BrakeStartPos;
            output.LeftForceB = Math.Clamp(
                (int)Math.Round(_config.MaxStrength * _brakeSmoothed), 0, 8);
        }

        if (_throttleSmoothed <= _config.ThrottleDeadzone)
        {
            output.RightMode = "Normal";
            output.RightForceA = 0;
            output.RightForceB = 0;
        }
        else
        {
            output.RightMode = "Resistance";
            output.RightForceA = _config.ThrottleStartPos;
            output.RightForceB = Math.Clamp(
                (int)Math.Round(_config.MaxStrength * _throttleSmoothed), 0, 8);
        }

        return output;
    }

    public void Reset()
    {
        _brakeSmoothed = 0;
        _throttleSmoothed = 0;
        _initialized = false;
    }
}

public struct TriggerOutput
{
    public string LeftMode;
    public int LeftForceA;
    public int LeftForceB;
    public string RightMode;
    public int RightForceA;
    public int RightForceB;
}
