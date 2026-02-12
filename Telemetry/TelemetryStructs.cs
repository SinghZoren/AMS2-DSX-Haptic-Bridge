using System.Runtime.InteropServices;

namespace Rf2DsxBridge.Telemetry;

public static class Rf2Constants
{
    public const int MAX_MAPPED_VEHICLES = 128;
    public const string MM_TELEMETRY_FILE_NAME = "$rFactor2SMMP_Telemetry$";
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct rF2Vec3
{
    public double x, y, z;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
public struct rF2Wheel
{
    public double mSuspensionDeflection;
    public double mRideHeight;
    public double mSuspForce;
    public double mBrakeTemp;
    public double mBrakePressure;
    public double mRotation;
    public double mLateralPatchVel;
    public double mLongitudinalPatchVel;
    public double mLateralGroundVel;
    public double mLongitudinalGroundVel;
    public double mCamber;
    public double mLateralForce;
    public double mLongitudinalForce;
    public double mTireLoad;
    public double mGripFract;
    public double mPressure;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public double[] mTemperature;
    public double mWear;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] mTerrainName;
    public byte mSurfaceType;
    public byte mFlat;
    public byte mDetached;
    public byte mStaticUndeflectedRadius;
    public double mVerticalTireDeflection;
    public double mWheelYLocation;
    public double mToe;
    public double mTireCarcassTemperature;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public double[] mTireInnerLayerTemperature;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
    public byte[] mExpansion;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
public struct rF2VehicleTelemetry
{
    public int mID;
    public double mDeltaTime;
    public double mElapsedTime;
    public int mLapNumber;
    public double mLapStartET;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] mVehicleName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] mTrackName;
    public rF2Vec3 mPos;
    public rF2Vec3 mLocalVel;
    public rF2Vec3 mLocalAccel;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public rF2Vec3[] mOri;
    public rF2Vec3 mLocalRot;
    public rF2Vec3 mLocalRotAccel;
    public int mGear;
    public double mEngineRPM;
    public double mEngineWaterTemp;
    public double mEngineOilTemp;
    public double mClutchRPM;
    public double mUnfilteredThrottle;
    public double mUnfilteredBrake;
    public double mUnfilteredSteering;
    public double mUnfilteredClutch;
    public double mFilteredThrottle;
    public double mFilteredBrake;
    public double mFilteredSteering;
    public double mFilteredClutch;
    public double mSteeringShaftTorque;
    public double mFront3rdDeflection;
    public double mRear3rdDeflection;
    public double mFrontWingHeight;
    public double mFrontRideHeight;
    public double mRearRideHeight;
    public double mDrag;
    public double mFrontDownforce;
    public double mRearDownforce;
    public double mFuel;
    public double mEngineMaxRPM;
    public byte mScheduledStops;
    public byte mOverheating;
    public byte mDetached;
    public byte mHeadlights;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] mDentSeverity;
    public double mLastImpactET;
    public double mLastImpactMagnitude;
    public rF2Vec3 mLastImpactPos;
    public double mEngineTorque;
    public int mCurrentSector;
    public byte mSpeedLimiter;
    public byte mMaxGears;
    public byte mFrontTireCompoundIndex;
    public byte mRearTireCompoundIndex;
    public double mFuelCapacity;
    public byte mFrontFlapActivated;
    public byte mRearFlapActivated;
    public byte mRearFlapLegalStatus;
    public byte mIgnitionStarter;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
    public byte[] mFrontTireCompoundName;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
    public byte[] mRearTireCompoundName;
    public byte mSpeedLimiterAvailable;
    public byte mAntiStallActivated;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] mUnused;
    public float mVisualSteeringWheelRange;
    public double mRearBrakeBias;
    public double mTurboBoostPressure;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] mPhysicsToGraphicsOffset;
    public float mPhysicalSteeringWheelRange;
    public double mBatteryChargeFraction;
    public double mElectricBoostMotorTorque;
    public double mElectricBoostMotorRPM;
    public double mElectricBoostMotorTemperature;
    public double mElectricBoostWaterTemperature;
    public byte mElectricBoostMotorState;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 111)]
    public byte[] mExpansion;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public rF2Wheel[] mWheels;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
public struct rF2Telemetry
{
    public uint mVersionUpdateBegin;
    public uint mVersionUpdateEnd;
    public int mBytesUpdatedHint;
    public int mNumVehicles;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Rf2Constants.MAX_MAPPED_VEHICLES)]
    public rF2VehicleTelemetry[] mVehicles;
}
