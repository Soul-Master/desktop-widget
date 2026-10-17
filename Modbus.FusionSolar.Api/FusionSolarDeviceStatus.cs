using System.Text.Json.Serialization;

namespace Modbus.FusionSolar.Api;

/// <summary>SUN2000 operating states from Huawei Solar 3.0.7 register_values.py (AGPL-3.0).</summary>
/// <remarks>Unknown codes are preserved as unnamed enum values; 0xFFFF is unavailable.</remarks>
[JsonConverter(typeof(JsonStringEnumConverter<FusionSolarDeviceStatus>))]
public enum FusionSolarDeviceStatus : ushort
{
    StandbyInitializing = 0x0000,
    StandbyDetectingInsulationResistance = 0x0001,
    StandbyDetectingIrradiation = 0x0002,
    StandbyGridDetecting = 0x0003,
    Starting = 0x0100,
    OnGrid = 0x0200,
    GridConnectionPowerLimited = 0x0201,
    GridConnectionSelfDerating = 0x0202,
    OffGridRunning = 0x0203,
    ShutdownFault = 0x0300,
    ShutdownCommand = 0x0301,
    ShutdownOvgr = 0x0302,
    ShutdownCommunicationDisconnected = 0x0303,
    ShutdownPowerLimited = 0x0304,
    ShutdownManualStartupRequired = 0x0305,
    ShutdownDcSwitchesDisconnected = 0x0306,
    ShutdownRapidCutoff = 0x0307,
    ShutdownInputUnderpowered = 0x0308,
    ShutdownEssEndOfDischarge = 0x030C,
    GridSchedulingCosphiPCurve = 0x0401,
    GridSchedulingQUCurve = 0x0402,
    GridSchedulingPfUCurve = 0x0403,
    GridSchedulingDryContact = 0x0404,
    GridSchedulingQPCurve = 0x0405,
    SpotCheckReady = 0x0500,
    SpotChecking = 0x0501,
    Inspecting = 0x0600,
    AfciSelfCheck = 0x0700,
    IvScanning = 0x0800,
    DcInputDetection = 0x0900,
    OffGridCharging = 0x0A00,
    StandbyNoIrradiation = 0xA000
}
