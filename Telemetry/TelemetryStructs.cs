namespace Ams2DsxBridge.Telemetry;

/// <summary>
/// AMS2 (Automobilista 2) shared memory constants and byte offsets.
/// Based on the pCARS2/AMS2 Shared Memory API (version 14).
/// Shared memory name: "$pcars2$"
/// Enable in AMS2: Options > Visuals > Hardware > Shared Memory = "Project Cars 2"
/// </summary>
public static class Ams2Constants
{
    public const string MM_SHARED_MEMORY_NAME = "$pcars2$";
    public const int SHARED_MEMORY_VERSION = 14;
    public const int BUFFER_SIZE = 24000;
    public const int TYRE_FL = 0;
    public const int TYRE_FR = 1;
    public const int TYRE_RL = 2;
    public const int TYRE_RR = 3;
}

/// <summary>
/// Byte offsets into the AMS2/pCARS2 shared memory buffer.
/// Calculated from the C struct layout with MSVC Pack=4 alignment.
/// ParticipantInfo[64] = 100 bytes each = 6400 bytes starting at offset 28.
/// </summary>
public static class Ams2Offsets
{
    // Header
    public const int Version = 0;                    // uint
    public const int BuildVersionNumber = 4;         // uint
    public const int GameState = 8;                  // uint
    public const int SessionState = 12;              // uint
    public const int RaceState = 16;                 // uint
    public const int ViewedParticipantIndex = 20;    // int
    public const int NumParticipants = 24;           // int

    // ParticipantInfo[64] at offset 28, each 100 bytes = 6400 total

    // Unfiltered input
    public const int UnfilteredThrottle = 6428;      // float 0-1
    public const int UnfilteredBrake = 6432;         // float 0-1
    public const int UnfilteredSteering = 6436;      // float -1 to 1
    public const int UnfilteredClutch = 6440;        // float 0-1

    // Car name / class
    public const int CarName = 6444;                 // char[64]
    public const int CarClassName = 6508;            // char[64]

    // Event info
    public const int LapsInEvent = 6572;             // uint
    public const int TrackLocation = 6576;           // char[64]
    public const int TrackVariation = 6640;          // char[64]
    public const int TrackLength = 6704;             // float

    // Timings (skipped for brevity, 6708 through 6796)

    // Flags
    public const int HighestFlagColour = 6800;       // uint
    public const int HighestFlagReason = 6804;       // uint

    // Pit info
    public const int PitMode = 6808;                 // uint
    public const int PitSchedule = 6812;             // uint

    // Car state
    public const int CarFlags = 6816;                // uint (bitmask)
    public const int OilTempCelsius = 6820;          // float
    public const int OilPressureKPa = 6824;          // float
    public const int WaterTempCelsius = 6828;        // float
    public const int WaterPressureKPa = 6832;        // float
    public const int FuelPressureKPa = 6836;         // float
    public const int FuelLevel = 6840;               // float 0-1
    public const int FuelCapacity = 6844;            // float litres
    public const int Speed = 6848;                   // float m/s
    public const int Rpm = 6852;                     // float
    public const int MaxRPM = 6856;                  // float
    public const int Brake = 6860;                   // float 0-1 (filtered)
    public const int Throttle = 6864;                // float 0-1 (filtered)
    public const int Clutch = 6868;                  // float 0-1 (filtered)
    public const int Steering = 6872;                // float -1 to 1 (filtered)
    public const int Gear = 6876;                    // int (-1=R, 0=N, 1+=forward)
    public const int NumGears = 6880;                // int
    public const int OdometerKM = 6884;              // float
    public const int AntiLockActive = 6888;          // bool (1 byte)
    // 3 bytes padding
    public const int LastOpponentCollisionIndex = 6892;     // int
    public const int LastOpponentCollisionMagnitude = 6896; // float
    public const int BoostActive = 6900;             // bool (1 byte)
    // 3 bytes padding
    public const int BoostAmount = 6904;             // float

    // Motion & device
    public const int Orientation = 6908;             // float[3] Euler angles
    public const int LocalVelocity = 6920;           // float[3] m/s (X=lat, Y=vert, Z=long)
    public const int WorldVelocity = 6932;           // float[3] m/s
    public const int AngularVelocity = 6944;         // float[3] rad/s (X=pitch, Y=yaw, Z=roll)
    public const int LocalAcceleration = 6956;       // float[3] m/s^2 (X=lat, Y=vert, Z=long)
    public const int WorldAcceleration = 6968;       // float[3] m/s^2
    public const int ExtentsCentre = 6980;           // float[3]

    // Wheels / tyres (arrays of 4: FL, FR, RL, RR)
    public const int TyreFlags = 6992;               // uint[4]
    public const int Terrain = 7008;                 // uint[4] (surface type enum)
    public const int TyreY = 7024;                   // float[4]
    public const int TyreRPS = 7040;                 // float[4] revolutions/sec
    public const int TyreSlipSpeed = 7056;           // float[4] (OBSOLETE)
    public const int TyreTemp = 7072;                // float[4] Celsius
    public const int TyreGrip = 7088;                // float[4] (OBSOLETE)
    public const int TyreHeightAboveGround = 7104;   // float[4]
    public const int TyreLateralStiffness = 7120;    // float[4] (OBSOLETE)
    public const int TyreWear = 7136;                // float[4] 0-1
    public const int BrakeDamage = 7152;             // float[4] 0-1
    public const int SuspensionDamage = 7168;        // float[4] 0-1
    public const int BrakeTempCelsius = 7184;        // float[4]
    public const int TyreTreadTemp = 7200;           // float[4] Kelvin
    public const int TyreLayerTemp = 7216;           // float[4] Kelvin
    public const int TyreCarcassTemp = 7232;         // float[4] Kelvin
    public const int TyreRimTemp = 7248;             // float[4] Kelvin
    public const int TyreInternalAirTemp = 7264;     // float[4] Kelvin

    // Car damage
    public const int CrashState = 7280;              // uint (enum)
    public const int AeroDamage = 7284;              // float 0-1
    public const int EngineDamage = 7288;            // float 0-1

    // Weather
    public const int AmbientTemperature = 7292;      // float Celsius
    public const int TrackTemperature = 7296;        // float Celsius
    public const int RainDensity = 7300;             // float 0-1

    // pCARS2 v8+ additions
    public const int SequenceNumber = 7320;          // uint (volatile, odd=writing)
    public const int WheelLocalPositionY = 7324;     // float[4]
    public const int SuspensionTravel = 7340;        // float[4] metres
    public const int SuspensionVelocity = 7356;      // float[4] m/s
    public const int AirPressure = 7372;             // float[4] PSI
    public const int EngineSpeed = 7388;             // float rad/s
    public const int EngineTorque = 7392;            // float Nm
    public const int Wings = 7396;                   // float[2] 0-1

    // AMS2 v11+ additions
    public const int AntiLockSetting = 20660;        // int (-1=unset)
    public const int TractionControlSetting = 20664; // int (-1=unset)
}

public static class Ams2GameState
{
    public const uint Exited = 0;
    public const uint FrontEnd = 1;
    public const uint InGamePlaying = 2;
    public const uint InGamePaused = 3;
    public const uint InGameInMenuTimeTicking = 4;
    public const uint InGameRestarting = 5;
    public const uint InGameReplay = 6;
    public const uint FrontEndReplay = 7;
}

[Flags]
public enum Ams2CarFlags : uint
{
    Headlight = 1 << 0,
    EngineActive = 1 << 1,
    EngineWarning = 1 << 2,
    SpeedLimiter = 1 << 3,
    Abs = 1 << 4,
    Handbrake = 1 << 5,
    TractionControl = 1 << 6,
}

public static class Ams2CrashState
{
    public const uint None = 0;
    public const uint Offtrack = 1;
    public const uint LargeProp = 2;
    public const uint Spinning = 3;
    public const uint Rolling = 4;
}
