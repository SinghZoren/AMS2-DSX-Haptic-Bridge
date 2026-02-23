# AMS2 -> DualSense Haptic Bridge

A Windows app that reads Automobilista 2 telemetry via shared memory and drives PS5 DualSense adaptive triggers (L2/R2) and rumble motors via native USB HID. Feel curbs, lockups, wheelspin, impacts, surface texture, and oversteer through your controller.

## What You Feel

| Situation | Triggers (L2/R2) | Rumble |
|-----------|------------------|--------|
| Braking | L2 resistance proportional to brake pressure | - |
| Throttle | R2 light resistance proportional to throttle | - |
| ABS / lockup | L2 rapid pulse (~40Hz) | Chatter on fine motor |
| Wheelspin / TC | R2 pulse (~30Hz) + increased resistance | Buzz on fine motor |
| Curb hit | - | Side-biased burst (left curb = left motor) |
| Road surface | - | Subtle texture that increases with speed |
| Wall impact | Short burst on both triggers | Strong thump, fast decay |
| Oversteer / spin | R2 vibration cue | Escalating chassis shake |
| Gear shift | Brief R2 vibration | Subtle kick |

All effects are layered with smoothing, noise gates, hysteresis, and soft clipping. Nothing pegs to max or buzzes constantly.

## Requirements

### 1. DualSense controller via USB

USB connection is required for full adaptive trigger + rumble fidelity. The app communicates directly with the controller via HID output reports -- no DualSenseX needed.

Supported controllers:
- DualSense (PID 0x0CE6)
- DualSense Edge (PID 0x0DF2)

### 2. AMS2 Shared Memory Enabled

In Automobilista 2:
1. Go to **Options > Visuals > Hardware**
2. Set **Shared Memory** to **"Project Cars 2"**
3. Restart the session if you're already in-game

### 3. .NET 8 Runtime

Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) if not installed. Or use the self-contained build which bundles the runtime.

## Quick Start

```
dotnet build
dotnet run
```

Or publish a standalone exe:
```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

1. Connect DualSense via USB
2. Enable shared memory in AMS2 (see above)
3. Start AMS2 and load into a session
4. Run `ams2-dsx-bridge.exe`
5. Drive and feel the effects

## Output Modes

| Mode | Set in config | What it does |
|------|---------------|--------------|
| `hid` (default) | `"outputMode": "hid"` | Native USB HID: full triggers + rumble |
| `dsx` (fallback) | `"outputMode": "dsx"` | DualSenseX text file: triggers only, no rumble, vibration falls back to resistance |

**Important:** Do not run DualSenseX simultaneously with HID mode. Two apps sending HID output reports will conflict.

## Configuration (appsettings.json)

```json
{
  "outputMode": "hid",
  "updateHz": 120,

  "brakeDeadzone": 0.02,
  "throttleDeadzone": 0.02,
  "brakeStartPos": 2,
  "throttleStartPos": 1,
  "maxBrakeStrength": 8,
  "maxThrottleStrength": 5,

  "absGripThreshold": 0.85,
  "absTriggerFreqHz": 40,
  "tcGripThreshold": 0.60,
  "tcTriggerFreqHz": 30,

  "oversteerThresholdDeg": 10.0,

  "masterRumbleGain": 1.0,
  "roadRumbleGain": 0.5,
  "curbRumbleGain": 1.0,
  "absRumbleGain": 0.8,
  "tcRumbleGain": 0.3,
  "engineRumbleGain": 0.3,
  "impactRumbleGain": 1.0,
  "spinRumbleGain": 0.4,

  "rumbleSmoothingAlpha": 0.5,
  "rumbleNoiseGate": 0.05
}
```

### Key settings

| Setting | Default | Description |
|---------|---------|-------------|
| `outputMode` | `hid` | `"hid"` for native USB, `"dsx"` for DualSenseX fallback |
| `updateHz` | `120` | Update rate in Hz (60-250) |
| `maxBrakeStrength` | `8` | Max L2 resistance (1-8) |
| `maxThrottleStrength` | `5` | Max R2 resistance (1-8) |
| `absGripThreshold` | `0.85` | Grip below which ABS feedback activates |
| `tcGripThreshold` | `0.60` | Grip below which TC feedback activates |
| `oversteerThresholdDeg` | `10.0` | Oversteer angle (degrees) to trigger spin cues |
| `masterRumbleGain` | `1.0` | Global rumble multiplier (0 = off, 2 = double) |
| `*RumbleGain` | varies | Per-effect rumble multiplier |
| `rumbleNoiseGate` | `0.05` | Below this level, rumble snaps to zero |
| `estimatedWheelbaseMeter` | `2.6` | Used for oversteer detection (tune per car) |

## Tuning Tips

- **Too much rumble?** Lower `masterRumbleGain` (e.g. 0.6)
- **Can't feel curbs?** Raise `curbRumbleGain` (e.g. 1.5) and/or lower `curbSuspVelocityThreshold`
- **ABS too sensitive?** Lower `absGripThreshold` (e.g. 0.75)
- **Triggers too stiff?** Lower `maxBrakeStrength` / `maxThrottleStrength`
- **Jitter at idle?** Raise `rumbleNoiseGate` (e.g. 0.08) and/or raise deadzones
- **Oversteer cues wrong car?** Adjust `estimatedWheelbaseMeter` and `estimatedMaxSteerAngleDeg`

## How It Works

AMS2 exposes telemetry via the `$pcars2$` shared memory mapped file (same format as Project CARS 2). The bridge reads this at 120 Hz and computes haptic effects:

```
AMS2 Shared Memory ($pcars2$) -> TelemetryFrame (per-tick snapshot)
  -> TriggerEffectsEngine (brake resistance, ABS, TC, impact, curb, gear shift)
  -> RumbleEffectsEngine  (road, curb, ABS, TC, engine, impact, spin, g-force)
  -> EffectMixer           (EMA smoothing, noise gate, trigger hysteresis)
  -> HidOutputSink         (USB HID report to DualSense)
```

### AMS2-Specific Adaptations

Since AMS2 uses the pCARS2 shared memory API (different from rF2's plugin-based telemetry), the bridge handles several differences:

- **Grip estimation:** AMS2 doesn't expose per-wheel grip fraction. Instead, the bridge uses the game's ABS/TCS flags from `mCarFlags` combined with acceleration data to estimate grip levels.
- **Impact detection:** No impact timestamp like rF2. Instead detects impacts from crash state transitions, collision magnitude changes, and damage increases.
- **Suspension velocity:** Provided directly by AMS2 (no manual computation needed).
- **Slip ratio:** Computed from tire RPS vs ground speed.
- **Data integrity:** Uses AMS2's sequence number to avoid reading mid-write data.

## Behavior

- **No AMS2 running:** triggers off, rumble silent, auto-reconnects every 2 seconds
- **Controller unplugged:** auto-reconnects when plugged back in
- **Ctrl+C:** clears all effects before exiting
- **CPU usage:** < 1% typical at 120Hz

## Credits

Fork of the rFactor 2 DualSense Haptic Bridge, adapted for Automobilista 2's pCARS2 shared memory API.
