// Register definitions from Huawei SUN2000MA V200R024C00SPC106, Issue 06 (2025-06-12).
// See REGISTER-SOURCES.md for provenance and limitations.
namespace Modbus.FusionSolar.Api;

/// <summary>Compile-time register names from the reviewed SUN2000MA register map.</summary>
/// <remarks>Enum values are Modbus starting addresses. Underscores separate original document groups.
/// Wire metadata and access markers are declared on each member. Device support and authorization still apply.</remarks>
public enum FusionSolarDataNames : ushort
{
    /// <summary>Nameplate name of the device, which was Model name in the earlier version</summary>
    [SolarDataNameMetadata(30000, FusionSolarDataType.String, 15)]
    Model = 30000,

    /// <summary>Device serial number, which comes from the electronic label of the system</summary>
    [SolarDataNameMetadata(30015, FusionSolarDataType.String, 10)]
    SerialNumber = 30015,

    [SolarDataNameMetadata(30025, FusionSolarDataType.String, 10)]
    ProductCode = 30025,

    /// <remarks>
    /// Character string reported: initial version VX00R00X (VR version by default); difference in certifciation registration: VX00R00XD01, VX00R00XD02... (D0X is provided by test and certifciation personnel); subject to change with country codes.
    /// </remarks>
    [SolarDataNameMetadata(30035, FusionSolarDataType.String, 15)]
    FirmwareVersion = 30035,

    /// <remarks>
    /// V200R023C10SPCXXX. The offering name and software version are combined on the display page.
    /// </remarks>
    [SolarDataNameMetadata(30050, FusionSolarDataType.String, 15)]
    SoftwareVersion = 30050,

    /// <remarks>
    /// High-order characters: main version; upgrade in case of incompatible changes; low- order characters: revision; upgrade in case of compatible changes; baseline version: D8.0; 0x00080000
    /// </remarks>
    [SolarDataNameMetadata(30068, FusionSolarDataType.UInt32, 2)]
    ProtocolVersionModbus = 30068,

    [SolarDataNameMetadata(30070, FusionSolarDataType.UInt16, 1)]
    ModelIdentifier = 30070,

    [SolarDataNameMetadata(30071, FusionSolarDataType.UInt16, 1, Gain = 1)]
    NumberOfPvStrings = 30071,

    [SolarDataNameMetadata(30072, FusionSolarDataType.UInt16, 1, Gain = 1)]
    NumberOfMPPTs = 30072,

    /// <remarks>
    /// Pn
    /// </remarks>
    [SolarDataNameMetadata(30073, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    RatedPower = 30073,

    /// <remarks>
    /// Read-only interface of 42027
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(30075, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    MaximumActivePowerPmax = 30075,

    /// <remarks>
    /// Read-only interface of 42025
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(30077, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kVA")]
    MaximumApparentPowerSmax = 30077,

    /// <remarks>
    /// Reported to the monitoring module to indicate the reactive power adjustment range. Except for model differences, feature data is updated when the grid code or derating changes.
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells; Unit spelling normalized from kVar.
    /// </remarks>
    [SolarDataNameMetadata(30079, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kvar")]
    RealTimeMaximumReactivePowerQmaxFedToThePowerGrid = 30079,

    /// <remarks>
    /// Reported to the monitoring module to indicate the reactive power adjustment range. Except for model differences, feature data is updated when the grid code or derating changes.
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells; Unit spelling normalized from kVar.
    /// </remarks>
    [SolarDataNameMetadata(30081, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kvar")]
    RealTimeMaximumReactivePowerNegativeQmaxAbsorbedFromThePowerGrid = 30081,

    /// <remarks>
    /// The default maximum active power is fxied on the nameplate of the device and will not change for the device. It is used as the upper limit of the reference range (42027). The relationship is as follows: 0 &lt; P ≤ S ≤ max max P ≤ S or 0 &lt; max real max real _ _ P ≤ P ≤ S ≤ max max real max _ S . max real _
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(30083, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    MaximumActivePowerPmaxReal = 30083,

    /// <remarks>
    /// The default maximum apparent power is fixed on the nameplate of the device and will not change for the device. It is used as the upper limit of the reference range (42025). The relationship is as follows: 0 &lt; P ≤ S ≤ max max P ≤ S or 0 &lt; max real max real _ _ P ≤ P ≤ S ≤ max max real max _ S . max real _
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(30085, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kVA")]
    MaximumApparentPowerSmaxReal = 30085,

    /// <remarks>
    /// Reported to FusionSolar; empty value for OEM devices; not displayed on the customer UI; used for combination with the software version.
    /// </remarks>
    [SolarDataNameMetadata(30089, FusionSolarDataType.String, 15)]
    OfferingName = 30089,

    /// <remarks>
    /// XX: two uppercase letters, indicating the sales area or application area of the product. It is related to the AC power system. CN: Chinese Mainland; EU: Europe; JP: Japan; US: North America (United States/ Canada/areas with the same power grid or certification requirements as the United States); UK: United Kingdom; default - CN/EU: all areas where CE certification requirements are applicable. See Huawei FusionSolar Smart PV Solution and Product Naming Specifications.
    /// </remarks>
    [SolarDataNameMetadata(30105, FusionSolarDataType.String, 2)]
    ProductSalesArea = 30105,

    /// <summary>Unique ID of the software release entity; used for processing the compatibility of grid codes</summary>
    [SolarDataNameMetadata(30107, FusionSolarDataType.UInt16, 1)]
    ProductSoftwareIdentifier = 30107,

    /// <summary>Sequence number of the version released by the software release entity; used for processing the compatibility of grid codes</summary>
    [SolarDataNameMetadata(30108, FusionSolarDataType.UInt16, 1)]
    ProductSoftwareVersion = 30108,

    /// <remarks>
    /// Protocol loading and verification are similar to those in CAN1.0 and CAN2.0.
    /// </remarks>
    [SolarDataNameMetadata(30109, FusionSolarDataType.UInt16, 1)]
    GridCodeProtocolVersion = 30109,

    /// <summary>Unique ID of a software version</summary>
    /// <remarks>
    /// Upgrade packages with different IDs cannot be used for upgrade between each other. (Broadcast upgrade feature code)
    /// </remarks>
    [SolarDataNameMetadata(30110, FusionSolarDataType.UInt16, 1)]
    UniqueSoftwareIdentifier = 30110,

    /// <remarks>
    /// Considering the increasing number of southbound devices to be upgraded, sufficient upgrade packages must be reserved.
    /// </remarks>
    [SolarDataNameMetadata(30111, FusionSolarDataType.UInt16, 1, Gain = 1)]
    QuantityOfSubpackagesInTheUpgradePackage = 30111,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30112, FusionSolarDataType.UInt32, 2)]
    Subpackage1Information = 30112,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30114, FusionSolarDataType.UInt32, 2)]
    Subpackage2Information = 30114,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30116, FusionSolarDataType.UInt32, 2)]
    Subpackage3Information = 30116,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30118, FusionSolarDataType.UInt32, 2)]
    Subpackage4Information = 30118,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30120, FusionSolarDataType.UInt32, 2)]
    Subpackage5Information = 30120,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30122, FusionSolarDataType.UInt32, 2)]
    Subpackage6Information = 30122,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30124, FusionSolarDataType.UInt32, 2)]
    Subpackage7Information = 30124,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30126, FusionSolarDataType.UInt32, 2)]
    Subpackage8Information = 30126,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30128, FusionSolarDataType.UInt32, 2)]
    Subpackage9Information = 30128,

    /// <remarks>
    /// High-order characters: file type ID; low-order characters: device type ID
    /// </remarks>
    [SolarDataNameMetadata(30130, FusionSolarDataType.UInt32, 2)]
    Subpackage10Information = 30130,

    /// <remarks>
    /// FusionSolar V800R021C10SP _ CXXX SUN2000: empty value _ for OEM devices; not displayed on the customer UI
    /// </remarks>
    [SolarDataNameMetadata(30136, FusionSolarDataType.String, 30)]
    SoftwarePackageName = 30136,

    /// <remarks>
    /// Indicates whether an optional hardware functional unit is confgi ured.
    /// 0: no
    /// 1: yes
    /// </remarks>
    [SolarDataNameMetadata(30206, FusionSolarDataType.Bitfield16, 1)]
    HardwareFunctionalUnitConfigurationIdentifier = 30206,

    [SolarDataNameMetadata(30207, FusionSolarDataType.Bitfield32, 2)]
    SubdeviceSupportFlag = 30207,

    [SolarDataNameMetadata(30209, FusionSolarDataType.Bitfield32, 2)]
    SubdevicePresenceFlag = 30209,

    /// <remarks>
    /// Review: Type and word count need review.
    /// </remarks>
    [SolarDataNameMetadata(30211, FusionSolarDataType.Enum16, 2)]
    FeatureCode1 = 30211,

    /// <remarks>
    /// Review: Type and word count need review.
    /// </remarks>
    [SolarDataNameMetadata(30213, FusionSolarDataType.Enum16, 2)]
    FeatureCode2 = 30213,

    /// <remarks>
    /// Review: Type and word count need review.
    /// </remarks>
    [SolarDataNameMetadata(30215, FusionSolarDataType.Enum16, 2)]
    FeatureCode3 = 30215,

    /// <remarks>
    /// Review: Type and word count need review.
    /// </remarks>
    [SolarDataNameMetadata(30217, FusionSolarDataType.Enum16, 2)]
    FeatureCode4 = 30217,

    [SolarDataNameMetadata(30219, FusionSolarDataType.Enum16, 1)]
    GridCodeMask1 = 30219,

    [SolarDataNameMetadata(30220, FusionSolarDataType.Enum16, 1)]
    GridCodeMask2 = 30220,

    [SolarDataNameMetadata(30221, FusionSolarDataType.Enum16, 1)]
    GridCodeMask3 = 30221,

    [SolarDataNameMetadata(30222, FusionSolarDataType.Enum16, 1)]
    GridCodeMask4 = 30222,

    [SolarDataNameMetadata(30223, FusionSolarDataType.Enum16, 1)]
    GridCodeMask5 = 30223,

    [SolarDataNameMetadata(30224, FusionSolarDataType.Enum16, 1)]
    GridCodeMask6 = 30224,

    [SolarDataNameMetadata(30225, FusionSolarDataType.Enum16, 1)]
    GridCodeMask7 = 30225,

    [SolarDataNameMetadata(30226, FusionSolarDataType.Enum16, 1)]
    GridCodeMask8 = 30226,

    [SolarDataNameMetadata(30227, FusionSolarDataType.Enum16, 1)]
    GridCodeMask9 = 30227,

    [SolarDataNameMetadata(30228, FusionSolarDataType.Enum16, 1)]
    GridCodeMask10 = 30228,

    [SolarDataNameMetadata(30229, FusionSolarDataType.Enum16, 1)]
    GridCodeMask11 = 30229,

    [SolarDataNameMetadata(30230, FusionSolarDataType.Enum16, 1)]
    GridCodeMask12 = 30230,

    [SolarDataNameMetadata(30231, FusionSolarDataType.Enum16, 1)]
    GridCodeMask13 = 30231,

    [SolarDataNameMetadata(30232, FusionSolarDataType.Enum16, 1)]
    GridCodeMask14 = 30232,

    [SolarDataNameMetadata(30233, FusionSolarDataType.Enum16, 1)]
    GridCodeMask15 = 30233,

    [SolarDataNameMetadata(30234, FusionSolarDataType.Enum16, 1)]
    GridCodeMask16 = 30234,

    [SolarDataNameMetadata(30235, FusionSolarDataType.Enum16, 1)]
    GridCodeMask17 = 30235,

    [SolarDataNameMetadata(30236, FusionSolarDataType.Enum16, 1)]
    GridCodeMask18 = 30236,

    [SolarDataNameMetadata(30237, FusionSolarDataType.Enum16, 1)]
    GridCodeMask19 = 30237,

    [SolarDataNameMetadata(30238, FusionSolarDataType.Enum16, 1)]
    GridCodeMask20 = 30238,

    [SolarDataNameMetadata(30239, FusionSolarDataType.Enum16, 1)]
    GridCodeMask21 = 30239,

    [SolarDataNameMetadata(30240, FusionSolarDataType.Enum16, 1)]
    GridCodeMask22 = 30240,

    [SolarDataNameMetadata(30241, FusionSolarDataType.Enum16, 1)]
    GridCodeMask23 = 30241,

    [SolarDataNameMetadata(30242, FusionSolarDataType.Enum16, 1)]
    GridCodeMask24 = 30242,

    [SolarDataNameMetadata(30243, FusionSolarDataType.Enum16, 1)]
    GridCodeMask25 = 30243,

    [SolarDataNameMetadata(30244, FusionSolarDataType.Enum16, 1)]
    GridCodeMask26 = 30244,

    [SolarDataNameMetadata(30245, FusionSolarDataType.Enum16, 1)]
    GridCodeMask27 = 30245,

    [SolarDataNameMetadata(30246, FusionSolarDataType.Enum16, 1)]
    GridCodeMask28 = 30246,

    [SolarDataNameMetadata(30247, FusionSolarDataType.Enum16, 1)]
    GridCodeMask29 = 30247,

    [SolarDataNameMetadata(30248, FusionSolarDataType.Enum16, 1)]
    GridCodeMask30 = 30248,

    [SolarDataNameMetadata(30249, FusionSolarDataType.Enum16, 1)]
    GridCodeMask31 = 30249,

    [SolarDataNameMetadata(30250, FusionSolarDataType.Enum16, 1)]
    GridCodeMask32 = 30250,

    /// <remarks>
    /// Review: Type and word count need review.
    /// </remarks>
    [SolarDataNameMetadata(30282, FusionSolarDataType.Enum16, 2)]
    FeatureCode5 = 30282,

    [SolarDataNameMetadata(30300, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask1Monitoring = 30300,

    [SolarDataNameMetadata(30301, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask2Monitoring = 30301,

    [SolarDataNameMetadata(30302, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask3Monitoring = 30302,

    [SolarDataNameMetadata(30303, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask4Monitoring = 30303,

    [SolarDataNameMetadata(30304, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask5Monitoring = 30304,

    [SolarDataNameMetadata(30305, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask6Monitoring = 30305,

    [SolarDataNameMetadata(30306, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask7Monitoring = 30306,

    [SolarDataNameMetadata(30307, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask8Monitoring = 30307,

    [SolarDataNameMetadata(30308, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask1Dsp = 30308,

    [SolarDataNameMetadata(30309, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask2Dsp = 30309,

    [SolarDataNameMetadata(30310, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask3Dsp = 30310,

    [SolarDataNameMetadata(30311, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask4Dsp = 30311,

    [SolarDataNameMetadata(30312, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask5Dsp = 30312,

    [SolarDataNameMetadata(30313, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask6Dsp = 30313,

    [SolarDataNameMetadata(30314, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask7Dsp = 30314,

    [SolarDataNameMetadata(30315, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask8Dsp = 30315,

    [SolarDataNameMetadata(30316, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask9Dsp = 30316,

    [SolarDataNameMetadata(30317, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask10Dsp = 30317,

    [SolarDataNameMetadata(30318, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask11Dsp = 30318,

    [SolarDataNameMetadata(30319, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask12Dsp = 30319,

    [SolarDataNameMetadata(30320, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask13Dsp = 30320,

    [SolarDataNameMetadata(30321, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask14Dsp = 30321,

    [SolarDataNameMetadata(30322, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask15Dsp = 30322,

    [SolarDataNameMetadata(30323, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask16Dsp = 30323,

    [SolarDataNameMetadata(30340, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask9Monitoring = 30340,

    [SolarDataNameMetadata(30341, FusionSolarDataType.Enum16, 1)]
    Level1ParameterMask10Monitoring = 30341,

    [SolarDataNameMetadata(30348, FusionSolarDataType.Enum16, 1)]
    BatteryParameterMask1Monitoring = 30348,

    [SolarDataNameMetadata(30350, FusionSolarDataType.Enum16, 1)]
    PidParameterMask1 = 30350,

    /// <summary>Last successful login time of the admin account on the app</summary>
    [SolarDataNameMetadata(30352, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    LastLoginTimeAppAdministratorPresetInstaller = 30352,

    /// <summary>Last successful login time of an advanced user account on the app</summary>
    [SolarDataNameMetadata(30354, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    LastLoginTimeAppCommonUserPresetUser = 30354,

    [SolarDataNameMetadata(31000, FusionSolarDataType.String, 15, Gain = 1)]
    InverterHardwareVersion = 31000,

    /// <summary>Electronic label of the monitoring board</summary>
    [SolarDataNameMetadata(31015, FusionSolarDataType.String, 10)]
    MonitoringBoardSerialNumber = 31015,

    /// <remarks>
    /// MCU1 version
    /// </remarks>
    [SolarDataNameMetadata(31025, FusionSolarDataType.String, 15)]
    MonitoringSoftwareVersion = 31025,

    /// <remarks>
    /// MCU2 version
    /// </remarks>
    [SolarDataNameMetadata(31040, FusionSolarDataType.String, 15)]
    MasterDspVersion = 31040,

    /// <remarks>
    /// MCU3 version
    /// </remarks>
    [SolarDataNameMetadata(31055, FusionSolarDataType.String, 15)]
    SlaveDspVersion = 31055,

    /// <remarks>
    /// MCU4 version
    /// </remarks>
    [SolarDataNameMetadata(31070, FusionSolarDataType.String, 15)]
    CpldVersion = 31070,

    /// <remarks>
    /// MCU5 version
    /// </remarks>
    [SolarDataNameMetadata(31085, FusionSolarDataType.String, 15)]
    AfciVersion = 31085,

    /// <remarks>
    /// MCU6 version
    /// </remarks>
    [SolarDataNameMetadata(31100, FusionSolarDataType.String, 15)]
    BuiltInPid = 31100,

    /// <remarks>
    /// MCU9 version
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(31145, FusionSolarDataType.String, 15)]
    AfciM4SoftwareVersion = 31145,

    /// <remarks>
    /// Review: Type and word count need review.
    /// </remarks>
    [SolarDataNameMetadata(31200, FusionSolarDataType.Enum16, 10)]
    RegistrationCode = 31200,

    /// <remarks>
    /// CRC16 value of the SN, which is the key data ID. It is used to prevent the energy yield data from being incorrectly modified due to incorrect energy yield reported by devices with the same address. The CRC value of the SN is added to ensure that the energy yield source is correct.
    /// </remarks>
    [SolarDataNameMetadata(32015, FusionSolarDataType.UInt16, 1)]
    DeviceSerialNumberFeatureCode = 32015,

    /// <remarks>
    /// When bit 9 of power parameter mask 14 is 0, the signal name is PV1 voltage. When bit 9 of power parameter mask 14 is 1, the following logic applies: When DC input display mode 30205 is 0, the signal name is PV1 voltage. When DC input display mode 30205 is 1, the signal name is MPPT1 voltage.
    /// </remarks>
    [SolarDataNameMetadata(32016, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv1Voltage = 32016,

    /// <remarks>
    /// When bit 9 of power parameter mask 14 is 0, the signal name is PV1 current. When bit 9 of power parameter mask 14 is 1, the following logic applies: When DC input display mode 30205 is 0, the signal name is PV1 current. When DC input display mode 30205 is 1, the signal name is MPPT1 current.
    /// </remarks>
    [SolarDataNameMetadata(32017, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv1Current = 32017,

    /// <remarks>
    /// When bit 9 of power parameter mask 14 is 0, the signal name is PV2 voltage. When bit 9 of power parameter mask 14 is 1, the following logic applies: When DC input display mode 30205 is 0, the signal name is PV2 voltage. When DC input display mode 30205 is 1, the signal name is MPPT2 voltage.
    /// </remarks>
    [SolarDataNameMetadata(32018, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv2Voltage = 32018,

    /// <remarks>
    /// When bit 9 of power parameter mask 14 is 0, the signal name is PV2 current. When bit 9 of power parameter mask 14 is 1, the following logic applies: When DC input display mode 30205 is 0, the signal name is PV2 current. When DC input display mode 30205 is 1, the signal name is MPPT2 current.
    /// </remarks>
    [SolarDataNameMetadata(32019, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv2Current = 32019,

    [SolarDataNameMetadata(32020, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv3Voltage = 32020,

    [SolarDataNameMetadata(32021, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv3Current = 32021,

    [SolarDataNameMetadata(32022, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv4Voltage = 32022,

    [SolarDataNameMetadata(32023, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv4Current = 32023,

    [SolarDataNameMetadata(32024, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv5Voltage = 32024,

    [SolarDataNameMetadata(32025, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv5Current = 32025,

    [SolarDataNameMetadata(32026, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv6Voltage = 32026,

    [SolarDataNameMetadata(32027, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv6Current = 32027,

    [SolarDataNameMetadata(32064, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    TotalInputPower = 32064,

    /// <remarks>
    /// When the output mode is L/N, the signal name is Grid voltage. When the output mode is L1/L2/N or L1/L2, the signal name is UW grid voltage.
    /// </remarks>
    [SolarDataNameMetadata(32066, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    ABLineVoltageOfGrid = 32066,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N, L1/L2/N, or L1/L2.
    /// </remarks>
    [SolarDataNameMetadata(32067, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    BCLineVoltageOfGrid = 32067,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N, L1/L2/N, or L1/L2.
    /// </remarks>
    [SolarDataNameMetadata(32068, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    CALineVoltageOfGrid = 32068,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N. When the output mode is L1/L2/N or L1/L2, the signal name is UO grid voltage. This parameter is not displayed in off-grid mode.
    /// </remarks>
    [SolarDataNameMetadata(32069, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    PhaseAVoltageOfGrid = 32069,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N. When the output mode is L1/L2/N or L1/L2, the signal name is WO grid voltage. This parameter is not displayed in off-grid mode.
    /// </remarks>
    [SolarDataNameMetadata(32070, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    PhaseBVoltageOfGrid = 32070,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N, L1/L2/N, or L1/L2.
    /// </remarks>
    [SolarDataNameMetadata(32071, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    PhaseCVoltageOfGrid = 32071,

    /// <remarks>
    /// When the output mode is L/N, L1/L2/N, or L1/L2, the signal name is Grid current.
    /// </remarks>
    [SolarDataNameMetadata(32072, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "A")]
    PhaseACurrentOfGrid = 32072,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N, L1/L2/N, or L1/L2.
    /// </remarks>
    [SolarDataNameMetadata(32074, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "A")]
    PhaseBCurrentOfGrid = 32074,

    /// <remarks>
    /// This parameter is invalid when the output mode is L/N, L1/L2/N, or L1/L2.
    /// </remarks>
    [SolarDataNameMetadata(32076, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "A")]
    PhaseCCurrentOfGrid = 32076,

    [SolarDataNameMetadata(32078, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    TodaySPeakActivePower = 32078,

    [SolarDataNameMetadata(32080, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    ActivePower = 32080,

    /// <remarks>
    /// Review: Unit spelling normalized from kVar.
    /// </remarks>
    [SolarDataNameMetadata(32082, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kvar")]
    ReactivePower = 32082,

    [SolarDataNameMetadata(32084, FusionSolarDataType.Int16, 1, Gain = 1000)]
    PowerFactor = 32084,

    [SolarDataNameMetadata(32085, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    GridFrequency = 32085,

    /// <remarks>
    /// Indicates the power generation efcfiiency when the PCS is used for power generation or the absorption efficiency when the PCS is used for power absorption.
    /// </remarks>
    [SolarDataNameMetadata(32086, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "%")]
    InverterEfficiency = 32086,

    [SolarDataNameMetadata(32087, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature = 32087,

    [SolarDataNameMetadata(32088, FusionSolarDataType.UInt16, 1, Gain = 1000, Unit = "MΩ")]
    InsulationResistance = 32088,

    /// <remarks>
    /// For details, see the Inverter Key Signal Extension Description.
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(32089, FusionSolarDataType.Enum16, 1)]
    DeviceStatus = 32089,

    /// <summary>Fault code corresponding to the alarm with the highest priority</summary>
    /// <remarks>
    /// For details, see the Alarm Description sheet.
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(32090, FusionSolarDataType.UInt16, 1)]
    ErrorCode = 32090,

    /// <remarks>
    /// Calculated by the monitoring module
    /// </remarks>
    [SolarDataNameMetadata(32091, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    StartupTime = 32091,

    /// <remarks>
    /// Calculated by the monitoring module
    /// </remarks>
    [SolarDataNameMetadata(32093, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    ShutdownTime = 32093,

    [SolarDataNameMetadata(32106, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    TotalYield = 32106,

    [SolarDataNameMetadata(32108, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    TotalDcInputEnergy = 32108,

    [SolarDataNameMetadata(32110, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    StatisticalTimeOfCurrentEnergyYield = 32110,

    [SolarDataNameMetadata(32112, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyYieldInCurrentHour = 32112,

    [SolarDataNameMetadata(32114, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    YieldToday = 32114,

    [SolarDataNameMetadata(32116, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    YieldThisMonth = 32116,

    [SolarDataNameMetadata(32118, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    YieldThisYear = 32118,

    /// <remarks>
    /// Review: PDF labels signal as power but gives kWh.
    /// </remarks>
    [SolarDataNameMetadata(32133, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kWh")]
    InverterPhaseAActivePower = 32133,

    /// <remarks>
    /// Review: PDF labels signal as power but gives kWh.
    /// </remarks>
    [SolarDataNameMetadata(32135, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kWh")]
    InverterPhaseBActivePower = 32135,

    /// <remarks>
    /// Review: PDF labels signal as power but gives kWh.
    /// </remarks>
    [SolarDataNameMetadata(32137, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kWh")]
    InverterPhaseCActivePower = 32137,

    /// <remarks>
    /// After an alarm is cleared locally, the SN increases by 1. After the alarm is inverted, the SN skips 0 and starts from 1.
    /// </remarks>
    [SolarDataNameMetadata(32155, FusionSolarDataType.UInt16, 1)]
    AlarmClearanceSerialNumber = 32155,

    /// <remarks>
    /// Including generated energy/ absorbed energy
    /// </remarks>
    [SolarDataNameMetadata(32156, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    StatisticalTimeOfEnergyInThePreviousHour = 32156,

    [SolarDataNameMetadata(32158, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyGeneratedInThePreviousHour = 32158,

    /// <remarks>
    /// Including generated energy/ absorbed energy
    /// </remarks>
    [SolarDataNameMetadata(32160, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    StatisticalTimeOfEnergyInThePreviousDay = 32160,

    [SolarDataNameMetadata(32162, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyYieldInThePreviousDay = 32162,

    /// <remarks>
    /// Including generated energy/ absorbed energy
    /// </remarks>
    [SolarDataNameMetadata(32164, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    StatisticalTimeOfEnergyInThePreviousMonth = 32164,

    [SolarDataNameMetadata(32166, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyYieldInThePreviousMonth = 32166,

    /// <remarks>
    /// Including generated energy/ absorbed energy
    /// </remarks>
    [SolarDataNameMetadata(32168, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    StatisticalTimeOfEnergyInThePreviousYear = 32168,

    [SolarDataNameMetadata(32170, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyYieldInThePreviousYear = 32170,

    /// <remarks>
    /// When a new active alarm is generated, the SN increases by 1.
    /// </remarks>
    [SolarDataNameMetadata(32172, FusionSolarDataType.UInt32, 2)]
    SerialNumberOfTheLatestActiveAlarm = 32172,

    /// <remarks>
    /// When an active alarm is moved to the historical alarm list, the SN of the historical alarm is the same as that of the last active alarm transferred to the historical alarm list.
    /// </remarks>
    [SolarDataNameMetadata(32174, FusionSolarDataType.UInt32, 2)]
    SerialNumberOfTheLatestHistoricalAlarm = 32174,

    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32176, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    TotalBusVoltage = 32176,

    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32177, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    HighestPvVoltage = 32177,

    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32178, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    LowestPvVoltage = 32178,

    /// <summary>Average voltage between PV – and ground</summary>
    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32179, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    AverageVoltageBetweenPvMinusAndGround = 32179,

    /// <summary>Highest voltage between PV + and ground</summary>
    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32180, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    HighestVoltageBetweenPvPlusAndGround = 32180,

    /// <summary>Lowest voltage between PV – and ground</summary>
    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32181, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    LowestVoltageBetweenPvMinusAndGround = 32181,

    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios. To be compatible with PID2.0, the PCS reports 1502.
    /// 0: 1000/1100 V inverter;
    /// 1500: HAV1 inverter;
    /// 1502: HAV2 inverter.
    /// </remarks>
    [SolarDataNameMetadata(32182, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "V")]
    InverterPeWithstandVoltage = 32182,

    /// <remarks>
    /// Reported by the PID module; used in single-stage scenarios.
    /// </remarks>
    [SolarDataNameMetadata(32183, FusionSolarDataType.Bitfield16, 1, Gain = 1)]
    IsoCharacteristicInformation = 32183,

    [SolarDataNameMetadata(32190, FusionSolarDataType.Enum16, 1)]
    RunningStatusOfBuiltInPid = 32190,

    /// <summary>PV – voltage to ground</summary>
    [SolarDataNameMetadata(32191, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    PvMinusVoltageToGround = 32191,

    /// <remarks>
    /// Displayed on the management system UI of TD Tech [5 points and 4 lines]
    /// </remarks>
    [SolarDataNameMetadata(32212, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    Mppt1DcTotalYield = 32212,

    /// <remarks>
    /// Displayed on the management system UI of TD Tech [5 points and 4 lines]
    /// </remarks>
    [SolarDataNameMetadata(32214, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    Mppt2DcTotalYield = 32214,

    /// <remarks>
    /// Displayed on the management system UI of TD Tech [5 points and 4 lines]
    /// </remarks>
    [SolarDataNameMetadata(32216, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    Mppt3DcTotalYield = 32216,

    /// <remarks>
    /// Displayed on the management system UI of TD Tech [5 points and 4 lines]
    /// </remarks>
    [SolarDataNameMetadata(32218, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    Mppt4DcTotalYield = 32218,

    /// <remarks>
    /// GroupID: 0xFF00
    /// </remarks>
    [SolarDataNameMetadata(32252, FusionSolarDataType.Bitfield16, 1)]
    MonitoringAlarm1 = 32252,

    /// <remarks>
    /// GroupID: 0xFF01
    /// </remarks>
    [SolarDataNameMetadata(32253, FusionSolarDataType.Bitfield16, 1)]
    MonitoringAlarm2 = 32253,

    /// <remarks>
    /// GroupID: 0xFF02
    /// </remarks>
    [SolarDataNameMetadata(32254, FusionSolarDataType.Bitfield16, 1)]
    MonitoringAlarm3 = 32254,

    /// <remarks>
    /// GroupID: 0x000D
    /// </remarks>
    [SolarDataNameMetadata(32268, FusionSolarDataType.Bitfield16, 1)]
    BuiltInPidAlarm = 32268,

    /// <remarks>
    /// GroupID: 0xFF03
    /// </remarks>
    [SolarDataNameMetadata(32271, FusionSolarDataType.Bitfield16, 1)]
    MonitoringAlarm4 = 32271,

    /// <remarks>
    /// GroupID: 0xFF04
    /// </remarks>
    [SolarDataNameMetadata(32272, FusionSolarDataType.Bitfield16, 1)]
    MonitoringAlarm5 = 32272,

    /// <remarks>
    /// GroupID: 0xFF05
    /// </remarks>
    [SolarDataNameMetadata(32274, FusionSolarDataType.Bitfield16, 1)]
    MonitoringAlarm6 = 32274,

    [SolarDataNameMetadata(32324, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt1TotalInputPower = 32324,

    [SolarDataNameMetadata(32326, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt2TotalInputPower = 32326,

    [SolarDataNameMetadata(32328, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt3TotalInputPower = 32328,

    [SolarDataNameMetadata(32330, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt4TotalInputPower = 32330,

    [SolarDataNameMetadata(32332, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt5TotalInputPower = 32332,

    [SolarDataNameMetadata(32334, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt6TotalInputPower = 32334,

    [SolarDataNameMetadata(32336, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt7TotalInputPower = 32336,

    [SolarDataNameMetadata(32338, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt8TotalInputPower = 32338,

    [SolarDataNameMetadata(32340, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    Mppt9TotalInputPower = 32340,

    [SolarDataNameMetadata(32344, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv25Voltage = 32344,

    [SolarDataNameMetadata(32345, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv25Current = 32345,

    [SolarDataNameMetadata(32346, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv26Voltage = 32346,

    [SolarDataNameMetadata(32347, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv26Current = 32347,

    [SolarDataNameMetadata(32348, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv27Voltage = 32348,

    [SolarDataNameMetadata(32349, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv27Current = 32349,

    [SolarDataNameMetadata(32350, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    Pv28Voltage = 32350,

    [SolarDataNameMetadata(32351, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "A")]
    Pv28Current = 32351,

    /// <remarks>
    /// Used for inspection data
    /// </remarks>
    [SolarDataNameMetadata(35000, FusionSolarDataType.UInt32, 2, Gain = 10, Unit = "hour")]
    CapacitorGroupRuntime = 35000,

    /// <remarks>
    /// Used for inspection data
    /// </remarks>
    [SolarDataNameMetadata(35002, FusionSolarDataType.UInt32, 2, Gain = 10, Unit = "hour")]
    InternalFan1Runtime = 35002,

    /// <remarks>
    /// Used for inspection data
    /// </remarks>
    [SolarDataNameMetadata(35004, FusionSolarDataType.UInt32, 2, Gain = 10, Unit = "hour")]
    InternalFan2Runtime = 35004,

    /// <remarks>
    /// Used for inspection data
    /// </remarks>
    [SolarDataNameMetadata(35006, FusionSolarDataType.UInt32, 2, Gain = 10, Unit = "hour")]
    InternalFan3Runtime = 35006,

    /// <remarks>
    /// Used for inspection data
    /// </remarks>
    [SolarDataNameMetadata(35008, FusionSolarDataType.UInt32, 2, Gain = 10, Unit = "hour")]
    InternalFan4Runtime = 35008,

    /// <remarks>
    /// Stored as R&amp;D data
    /// </remarks>
    [SolarDataNameMetadata(35010, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "V")]
    InternalAlarm = 35010,

    /// <summary>Temperature of inverter module A</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35021, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature1 = 35021,

    /// <summary>Temperature of inverter module B</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35022, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature2 = 35022,

    /// <summary>Temperature of inverter module C</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35023, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature3 = 35023,

    /// <summary>Sampled temperature of reverse polarity prevention module 1</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35024, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature4 = 35024,

    /// <summary>Ambient temperature of the output board relay - highest temperature</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35025, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature5 = 35025,

    /// <summary>Sampled temperature of the output board, power board input, and power board inverter – highest temperature</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35026, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature6 = 35026,

    /// <summary>Sampled temperature of reverse polarity prevention module 2</summary>
    /// <remarks>
    /// recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35027, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature7 = 35027,

    /// <summary>DC terminal temperature 1/2: highest temperature, recorded as R&amp;D 5-minute data</summary>
    [SolarDataNameMetadata(35028, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature8 = 35028,

    /// <summary>AC terminal temperature 1/2/3: highest temperature, recorded as R&amp;D 5-minute data</summary>
    [SolarDataNameMetadata(35029, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature9 = 35029,

    /// <remarks>
    /// Stored as R&amp;D data
    /// </remarks>
    [SolarDataNameMetadata(35030, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature10 = 35030,

    /// <remarks>
    /// Stored as R&amp;D data
    /// </remarks>
    [SolarDataNameMetadata(35031, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature11 = 35031,

    /// <remarks>
    /// Stored as R&amp;D data
    /// </remarks>
    [SolarDataNameMetadata(35032, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    InternalTemperature12 = 35032,

    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35038, FusionSolarDataType.Int16, 1, Gain = 1000, Unit = "A")]
    PhaseADcComponentDci = 35038,

    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35039, FusionSolarDataType.Int16, 1, Gain = 1000, Unit = "A")]
    PhaseBDcComponentDci = 35039,

    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35040, FusionSolarDataType.Int16, 1, Gain = 1000, Unit = "A")]
    PhaseCDcComponentDci = 35040,

    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35041, FusionSolarDataType.Int16, 1, Gain = 1, Unit = "mA")]
    LeakageCurrentRcd = 35041,

    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35042, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    PositiveBusVoltage = 35042,

    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35043, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    NegativeBusVoltage = 35043,

    /// <summary>Voltage between bus – and ground</summary>
    /// <remarks>
    /// Recorded as R&amp;D 5-minute data
    /// </remarks>
    [SolarDataNameMetadata(35044, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    VoltageBetweenBusMinusAndGround = 35044,

    [SolarDataNameMetadata(35094, FusionSolarDataType.Enum16, 1)]
    IVCurveScanningStatus = 35094,

    [SolarDataNameMetadata(35095, FusionSolarDataType.Enum16, 1)]
    IVCurveScanningCapability = 35095,

    [SolarDataNameMetadata(35115, FusionSolarDataType.Enum16, 1)]
    DelayedActivationStatus = 35115,

    /// <remarks>
    /// For details, see the description of "Structure Data."
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(35300, FusionSolarDataType.MultiData, 4)]
    ActivePowerAdjustmentStatus = 35300,

    /// <remarks>
    /// For details, see the description of "Structure Data."
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(35304, FusionSolarDataType.MultiData, 4)]
    ReactivePowerAdjustmentStatus = 35304,

    [SolarDataNameMetadata(37100, FusionSolarDataType.Enum16, 1)]
    MeterStatus = 37100,

    [SolarDataNameMetadata(37101, FusionSolarDataType.Int32, 2, Gain = 10, Unit = "V")]
    MeterGridVoltageGridPhaseAVoltage = 37101,

    [SolarDataNameMetadata(37103, FusionSolarDataType.Int32, 2, Gain = 10, Unit = "V")]
    MeterPhaseBVoltageOfGrid = 37103,

    [SolarDataNameMetadata(37105, FusionSolarDataType.Int32, 2, Gain = 10, Unit = "V")]
    MeterPhaseCVoltageOfGrid = 37105,

    [SolarDataNameMetadata(37107, FusionSolarDataType.Int32, 2, Gain = 100, Unit = "A")]
    MeterGridCurrentGridPhaseACurrent = 37107,

    [SolarDataNameMetadata(37109, FusionSolarDataType.Int32, 2, Gain = 100, Unit = "A")]
    MeterPhaseBCurrentOfGrid = 37109,

    [SolarDataNameMetadata(37111, FusionSolarDataType.Int32, 2, Gain = 100, Unit = "A")]
    MeterPhaseCCurrentOfGrid = 37111,

    [SolarDataNameMetadata(37113, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    MeterActivePower = 37113,

    /// <remarks>
    /// Review: Unit spelling normalized from Var.
    /// </remarks>
    [SolarDataNameMetadata(37115, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "var")]
    MeterReactivePower = 37115,

    [SolarDataNameMetadata(37117, FusionSolarDataType.Int16, 1, Gain = 1000)]
    MeterPowerFactor = 37117,

    [SolarDataNameMetadata(37118, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "Hz")]
    MeterGridFrequency = 37118,

    /// <remarks>
    /// Review: PDF labels signal as power but gives kWh.
    /// </remarks>
    [SolarDataNameMetadata(37119, FusionSolarDataType.Int32, 2, Gain = 100, Unit = "kWh")]
    MeterPositiveActivePower = 37119,

    /// <remarks>
    /// Review: PDF labels signal as power but gives kWh.
    /// </remarks>
    [SolarDataNameMetadata(37121, FusionSolarDataType.Int32, 2, Gain = 100, Unit = "kWh")]
    MeterReverseActivePower = 37121,

    /// <remarks>
    /// Review: Unit spelling normalized from kVarh.
    /// </remarks>
    [SolarDataNameMetadata(37123, FusionSolarDataType.Int32, 2, Gain = 100, Unit = "kvarh")]
    MeterCumulativeReactiveEnergy = 37123,

    [SolarDataNameMetadata(37125, FusionSolarDataType.Enum16, 1)]
    MeterType = 37125,

    [SolarDataNameMetadata(37126, FusionSolarDataType.Int32, 2, Gain = 10, Unit = "V")]
    MeterABLineVoltage = 37126,

    [SolarDataNameMetadata(37128, FusionSolarDataType.Int32, 2, Gain = 10, Unit = "V")]
    MeterBCLineVoltage = 37128,

    [SolarDataNameMetadata(37130, FusionSolarDataType.Int32, 2, Gain = 10, Unit = "V")]
    MeterCALineVoltage = 37130,

    [SolarDataNameMetadata(37132, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    MeterPhaseAActivePower = 37132,

    [SolarDataNameMetadata(37134, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    MeterPhaseBActivePower = 37134,

    [SolarDataNameMetadata(37136, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    MeterPhaseCActivePower = 37136,

    [SolarDataNameMetadata(37138, FusionSolarDataType.Enum16, 1)]
    MeterTypeCheck = 37138,

    [SolarDataNameMetadata(37518, FusionSolarDataType.Bitfield16, 1)]
    InverterOverallStatus = 37518,

    [SolarDataNameMetadata(37519, FusionSolarDataType.UInt16, 1, Gain = 1)]
    InverterOverallStatusSupportFlag = 37519,

    /// <remarks>
    /// The data is in epoch seconds of the local time.
    /// </remarks>
    [SolarDataNameMetadata(40000, FusionSolarDataType.EpochSeconds, 2, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    SystemTimeLocalTime = 40000,

    [SolarDataNameMetadata(40037, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    QUCharacteristicCurveMode = 40037,

    [SolarDataNameMetadata(40038, FusionSolarDataType.Int16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    PowerPercentageForTriggeringQUScheduling = 40038,

    [SolarDataNameMetadata(40120, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "kW")]
    [FusionSolarWrite]
    FixedActivePowerDeratingInKilowatts = 40120,

    [SolarDataNameMetadata(40122, FusionSolarDataType.Int16, 1, Gain = 1000)]
    [FusionSolarWrite]
    PowerFactorSetting = 40122,

    /// <remarks>
    /// The device converts the value to a fixed value of Q for reactive power control. S indicates S . max
    /// </remarks>
    [SolarDataNameMetadata(40123, FusionSolarDataType.Int16, 1, Gain = 1000)]
    [FusionSolarWrite]
    ReactivePowerCompensationQSLowPrecision = 40123,

    /// <remarks>
    /// Change requirement: The "Reactive power adjustment time" broadcast interface should be added to the delivery UI of the Q-P characteristic curve and cosphi-P characteristic curve, which is the same as the Q-U characteristic curve 20190918.
    /// </remarks>
    [SolarDataNameMetadata(40124, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    ReactivePowerAdjustmentTime = 40124,

    /// <remarks>
    /// Interface for fine adjustment of active power
    /// </remarks>
    [SolarDataNameMetadata(40125, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    ActivePowerDeratingByPercentageLowPrecision = 40125,

    /// <remarks>
    /// [0, P ] max
    /// </remarks>
    [SolarDataNameMetadata(40126, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    [FusionSolarWrite]
    FixedActivePowerDeratingInWatts = 40126,

    /// <remarks>
    /// The device converts the value to a fixed value of Q for reactive power control. S indicates S . max
    /// </remarks>
    [SolarDataNameMetadata(40128, FusionSolarDataType.Int16, 1, Gain = 1000)]
    [FusionSolarWrite]
    ReactivePowerCompensationAtNightQS = 40128,

    /// <remarks>
    /// Review: Unit spelling normalized from kVar.
    /// </remarks>
    [SolarDataNameMetadata(40129, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kvar")]
    [FusionSolarWrite]
    FixedNighttimeReactivePower = 40129,

    /// <summary>cos φ-P / Pn characteristic curve</summary>
    /// <remarks>
    /// See the Inverter Key Signal Extension Description.
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(40133, FusionSolarDataType.MultiData, 21)]
    [FusionSolarWrite]
    CosPhiPPnCharacteristicCurve = 40133,

    /// <remarks>
    /// See the Inverter Key Signal Extension Description.
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(40154, FusionSolarDataType.MultiData, 21)]
    [FusionSolarWrite]
    QUCharacteristicCurve = 40154,

    /// <remarks>
    /// See the Inverter Key Signal Extension Description.
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(40175, FusionSolarDataType.MultiData, 21)]
    [FusionSolarWrite]
    PfUCharacteristicCurve = 40175,

    /// <remarks>
    /// Used for overtemperature derating with the STS
    /// </remarks>
    [SolarDataNameMetadata(40197, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    ApparentPowerInPercentage = 40197,

    [SolarDataNameMetadata(40198, FusionSolarDataType.Int16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    PowerPercentageForExitingQUScheduling = 40198,

    /// <remarks>
    /// Used in distributed mode. The active power percentage control interface is delivered to the power software in backflow prevention control to control the upper limit of the output active power during underfrequency- caused power raising.
    /// </remarks>
    [SolarDataNameMetadata(40199, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    ActivePowerPercentageControlLowPrecision = 40199,

    [SolarDataNameMetadata(40200, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    Startup = 40200,

    [SolarDataNameMetadata(40201, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    Shutdown = 40201,

    /// <remarks>
    /// After receiving the command, the DSP responds immediately and then resets. After receiving the command, the inverter monitoring module sends the command to the DSP. After receiving a normal response, the inverter monitoring module responds to the northbound port and resets 3s later. The device does not restart if the reply fails.
    /// </remarks>
    [SolarDataNameMetadata(40205, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    Reset = 40205,

    /// <remarks>
    /// See the Inverter Key Signal Extension Description.
    /// Review: Scope references a separate signal/structure description.
    /// </remarks>
    [SolarDataNameMetadata(40354, FusionSolarDataType.MultiData, 21)]
    [FusionSolarWrite]
    QPCharacteristicCurve = 40354,

    /// <remarks>
    /// This interface is used to limit the output reactive power of the Q-U curve by limiting the current PF value. For the country codes for which this function is not required, the value is set to 0 by default, indicating that the reactive power output is not limited. EN 50549 requires that the value ranges from 0 to 1 and is 0.9 by default.
    /// </remarks>
    [SolarDataNameMetadata(40375, FusionSolarDataType.UInt16, 1, Gain = 1000)]
    [FusionSolarWrite]
    MinimumPfOfQUCharacteristicCurve = 40375,

    /// <remarks>
    /// After the voltage value on the Q-U curve reaches the trigger voltage, the reactive power changes after a delay time. Italy CEI0-16/21 requires that the default value be 3s, indicating that the Q-U curve takes effect 3s after the trigger voltage is reached. The delay can be set on the UI. For other country codes, the default value is 0.
    /// </remarks>
    [SolarDataNameMetadata(40376, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    DelayForQUCharacteristicCurveToTakeEffect = 40376,

    /// <remarks>
    /// CHINA MV800. For details _ about the value range, see the Grid Codes.
    /// </remarks>
    [SolarDataNameMetadata(42000, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    GridCode = 42000,

    /// <remarks>
    /// Displayed as a read-only signal on the customer UI. Currently, the 8.0 PCS supports only the three- phase three-wire system and the parameter does not need to be set. If the setting is supported, an interface change notifciation will be sent to each UI involved.
    /// </remarks>
    [SolarDataNameMetadata(42001, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    OutputMode = 42001,

    /// <remarks>
    /// Vn
    /// </remarks>
    [SolarDataNameMetadata(42002, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "V")]
    [FusionSolarWrite]
    VoltageLevel = 42002,

    /// <remarks>
    /// Fn
    /// </remarks>
    [SolarDataNameMetadata(42003, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyLevel = 42003,

    /// <remarks>
    /// If this parameter is disabled, the inverter is locked.
    /// </remarks>
    [SolarDataNameMetadata(42014, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    RemotePowerScheduling = 42014,

    [SolarDataNameMetadata(42015, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "%/s")]
    [FusionSolarWrite]
    ReactivePowerChangeGradient = 42015,

    /// <summary>Limits the speed of power change caused by a power scheduling command</summary>
    [SolarDataNameMetadata(42017, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "%/s")]
    [FusionSolarWrite]
    ActivePowerChangeGradient = 42017,

    /// <remarks>
    /// The value 0 indicates that the command is valid permanently.
    /// </remarks>
    [SolarDataNameMetadata(42019, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    ScheduleInstructionValidDuration = 42019,

    /// <remarks>
    /// [Maximum active power, S ] max
    /// </remarks>
    [SolarDataNameMetadata(42021, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kVA")]
    [FusionSolarWrite]
    MaximumApparentPower = 42021,

    /// <remarks>
    /// [0.1,P ] max
    /// </remarks>
    [SolarDataNameMetadata(42023, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    MaximumActivePower = 42023,

    /// <remarks>
    /// Sn. Upper limit of the maximum active power (P ); also used as the max reference for reactive power scheduling (Q/S).
    /// Review: Unit spelling normalized from kVar.
    /// </remarks>
    [SolarDataNameMetadata(42025, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kvar")]
    [FusionSolarWrite]
    ApparentPowerBaseline = 42025,

    /// <remarks>
    /// Lower limit of the maximum apparent power (S ); also max used as the reference for active scheduling (percentage).
    /// </remarks>
    [SolarDataNameMetadata(42027, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    ActivePowerBaseline = 42027,

    /// <remarks>
    /// According to Chinese standards, the active power change caused by irradiance fluctuation must meet certain speed requirements when the device is running properly.
    /// </remarks>
    [SolarDataNameMetadata(42029, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "min/100%")]
    [FusionSolarWrite]
    PlantActivePowerGradient = 42029,

    [SolarDataNameMetadata(42030, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    AverageActivePowerFilteringTime = 42030,

    [SolarDataNameMetadata(42032, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "s")]
    [FusionSolarWrite]
    PfUVoltageDetectionFilterTime = 42032,

    [SolarDataNameMetadata(42037, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    FrequencyDetectionFilterTime = 42037,

    [SolarDataNameMetadata(42040, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    RecoveryDelayOfFrequencyBasedActivePowerDerating = 42040,

    [SolarDataNameMetadata(42041, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    ExecutionDelayOfFrequencyBasedActivePowerDerating = 42041,

    [SolarDataNameMetadata(42042, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    HysteresisOfFrequencyBasedActivePowerDerating = 42042,

    [SolarDataNameMetadata(42043, FusionSolarDataType.UInt16, 1, Gain = 1000, Unit = "Hz")]
    [FusionSolarWrite]
    ResponseDeadbandOfFrequencyBasedControl = 42043,

    [SolarDataNameMetadata(42044, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    OnOffGridSwitchingMode = 42044,

    [SolarDataNameMetadata(42045, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    OffGridMode = 42045,

    [SolarDataNameMetadata(42046, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PqMode = 42046,

    [SolarDataNameMetadata(42047, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PvModuleType = 42047,

    [SolarDataNameMetadata(42048, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PidCompensationDirection = 42048,

    [SolarDataNameMetadata(42049, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    StringConnectionMode = 42049,

    /// <remarks>
    /// Displayed as a read-only signal on the customer UI. Currently, 8.0 PCS supports only "input not grounded, with transformer" and the parameter does not need to be set. If the setting is supported, an interface change notification will be sent to each UI involved.
    /// </remarks>
    [SolarDataNameMetadata(42050, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    IsolationSettings = 42050,

    [SolarDataNameMetadata(42051, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%/min")]
    [FusionSolarWrite]
    PowerChangeGradientOfFrequencyBasedControl = 42051,

    [SolarDataNameMetadata(42052, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    PowerChangeLimitOfFrequencyBasedControl = 42052,

    /// <remarks>
    /// This parameter is required in Poland and can be set. Initial delay in the frequency sensitive mode
    /// </remarks>
    [SolarDataNameMetadata(42053, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    DelayResponseTimeOfFrequencyBasedControl = 42053,

    [SolarDataNameMetadata(42054, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    MpptMultiPeakScanning = 42054,

    [SolarDataNameMetadata(42055, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "min")]
    [FusionSolarWrite]
    MpptScanningInterval = 42055,

    [SolarDataNameMetadata(42056, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    PredictedMpptPower = 42056,

    [SolarDataNameMetadata(42061, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    AutoStartUponGridRecovery = 42061,

    /// <summary>Shutdown at 0% power limit</summary>
    [SolarDataNameMetadata(42062, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ShutdownAt0PercentPowerLimit = 42062,

    /// <remarks>
    /// In a parallel system, 8.0 PCS automatically shuts down upon a communication interruption by default. The communication interruption duration is 1 minute. The customer-defined parameters and default power policy priority are determined by the PCS power. All parameters under Communication disconnection fail-safe are affected.
    /// </remarks>
    [SolarDataNameMetadata(42063, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ShutdownOnCommunicationFailure = 42063,

    [SolarDataNameMetadata(42064, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    StartupArrayUponCommunicationRecovery = 42064,

    [SolarDataNameMetadata(42065, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PowerQualityOptimizationMode = 42065,

    [SolarDataNameMetadata(42066, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    RcdEnhancing = 42066,

    [SolarDataNameMetadata(42067, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ReactivePowerOutputAtNight = 42067,

    [SolarDataNameMetadata(42069, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PidProtectionAtNight = 42069,

    [SolarDataNameMetadata(42070, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ApplyNighttimeReactivePowerParameters = 42070,

    /// <remarks>
    /// This parameter is displayed when Communication disconnection fail-safe is set to Enable.
    /// </remarks>
    [SolarDataNameMetadata(42072, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    CommunicationDisconnectionDetectionTime = 42072,

    [SolarDataNameMetadata(42073, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Afci = 42073,

    [SolarDataNameMetadata(42074, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    AfciDetectionMode = 42074,

    /// <summary>Used to initiate security protection after the northbound communication of the device is interrupted</summary>
    [SolarDataNameMetadata(42075, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    CommunicationDisconnectionFailSafe = 42075,

    /// <remarks>
    /// This parameter is displayed when Communication disconnection fail-safe is set to Enable.
    /// </remarks>
    [SolarDataNameMetadata(42076, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ActivePowerModeWhenCommunicationFails = 42076,

    /// <remarks>
    /// This parameter is displayed when Communication disconnection fail-safe is set to Enable and Active power mode when communication fails is set to a fixed value.
    /// </remarks>
    [SolarDataNameMetadata(42077, FusionSolarDataType.UInt32, 2, Gain = 10, Unit = "kW")]
    [FusionSolarWrite]
    ActivePowerThresholdWhenCommunicationFailsKWLowPrecision = 42077,

    /// <remarks>
    /// This parameter is displayed when Communication disconnection fail-safe is set to Enable.
    /// </remarks>
    [SolarDataNameMetadata(42079, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ReactivePowerModeWhenCommunicationFails = 42079,

    [SolarDataNameMetadata(42080, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    FrequencyChangeRateProtection = 42080,

    [SolarDataNameMetadata(42081, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "Hz/s")]
    [FusionSolarWrite]
    FrequencyChangeRateProtectionThreshold = 42081,

    [SolarDataNameMetadata(42082, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "s")]
    [FusionSolarWrite]
    DurationThresholdForFrequencyChangeRateProtection = 42082,

    /// <remarks>
    /// (Delivered Q/S value). This parameter is displayed when Communication disconnection fail-safe is set to Enable and Reactive power mode when communication fails is set to Q/S.
    /// </remarks>
    [SolarDataNameMetadata(42083, FusionSolarDataType.Int16, 1, Gain = 1000)]
    [FusionSolarWrite]
    ReactivePowerLimitWhenCommunicationFailsQSLowPrecision = 42083,

    /// <remarks>
    /// Parameter for normal reconnection
    /// </remarks>
    [SolarDataNameMetadata(42084, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    MaximumGridVoltageForGridConnection = 42084,

    /// <remarks>
    /// Parameter for normal reconnection
    /// </remarks>
    [SolarDataNameMetadata(42085, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    MinimumGridVoltageForGridConnection = 42085,

    /// <remarks>
    /// Parameter for normal reconnection
    /// </remarks>
    [SolarDataNameMetadata(42086, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    MaximumGridFrequencyForGridConnection = 42086,

    /// <remarks>
    /// Parameter for normal reconnection
    /// </remarks>
    [SolarDataNameMetadata(42087, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    MinimumGridFrequencyForGridConnection = 42087,

    /// <remarks>
    /// Parameter for reconnection after a fault occurs
    /// </remarks>
    [SolarDataNameMetadata(42088, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    MaximumGridVoltageForGridReconnection = 42088,

    /// <remarks>
    /// Parameter for reconnection after a fault occurs
    /// </remarks>
    [SolarDataNameMetadata(42089, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    MinimumGridVoltageForGridReconnection = 42089,

    /// <remarks>
    /// Parameter for reconnection after a fault occurs
    /// </remarks>
    [SolarDataNameMetadata(42090, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    MaximumGridFrequencyForGridReconnection = 42090,

    /// <remarks>
    /// Parameter for reconnection after a fault occurs
    /// </remarks>
    [SolarDataNameMetadata(42091, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    MinimumGridFrequencyForGridReconnection = 42091,

    [SolarDataNameMetadata(42092, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    DelayForAutomaticGridReconnection = 42092,

    /// <remarks>
    /// Short-circuit current I (STC) sc on the PV module template
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(42093, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "A")]
    [FusionSolarWrite]
    PvModuleNameplateShortCircuitCurrentStcIsc = 42093,

    /// <remarks>
    /// The value range on the customer UI is [0.02, 1.5]. The actual protection is performed by the inverter based on the DC voltage. The value range is [0.02, 1.5] for the 600 V DC inverter, [0.033, 1.5] for the 1000 V DC inverter, and [0.05, 1.5] for the 1500 V DC inverter.
    /// </remarks>
    [SolarDataNameMetadata(42097, FusionSolarDataType.UInt16, 1, Gain = 1000, Unit = "MΩ")]
    [FusionSolarWrite]
    InsulationResistanceProtectionThreshold = 42097,

    [SolarDataNameMetadata(42098, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    VoltageImbalanceProtectionThresholdPercent = 42098,

    [SolarDataNameMetadata(42099, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "°")]
    [FusionSolarWrite]
    PhaseProtectionThreshold = 42099,

    /// <remarks>
    /// Power raising gradient when the device is started after the power grid is faulty.
    /// </remarks>
    [SolarDataNameMetadata(42100, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    SoftStartTimeAfterGridFailure = 42100,

    /// <summary>cos φ-P / Pn trigger voltage</summary>
    [SolarDataNameMetadata(42101, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    CosPhiPPnTriggerVoltage = 42101,

    /// <summary>cos φ-P / Pn exit voltage</summary>
    [SolarDataNameMetadata(42102, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    CosPhiPPnExitVoltage = 42102,

    [SolarDataNameMetadata(42103, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    SoftStartTime = 42103,

    [SolarDataNameMetadata(42104, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    GridConnectionDelayAfterGridRecovery = 42104,

    /// <remarks>
    /// This parameter is configurable if Quick startup after grid fault is enabled.
    /// </remarks>
    [SolarDataNameMetadata(42105, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    DurationForDeterminingShortTimeGridDisconnection = 42105,

    [SolarDataNameMetadata(42107, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "%/s")]
    [FusionSolarWrite]
    ShutdownGradient = 42107,

    [SolarDataNameMetadata(42109, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    LineLossCompensation = 42109,

    [SolarDataNameMetadata(42110, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ZeroCurrentModeOnPowerGridFault = 42110,

    /// <remarks>
    /// This parameter can be set only for the VDE4120.
    /// </remarks>
    [SolarDataNameMetadata(42111, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    GridVoltageTripTriggeringThreshold = 42111,

    [SolarDataNameMetadata(42112, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Hvrt = 42112,

    [SolarDataNameMetadata(42113, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    HvrtTriggeringThreshold = 42113,

    [SolarDataNameMetadata(42114, FusionSolarDataType.UInt16, 1, Gain = 10)]
    [FusionSolarWrite]
    PositiveSequenceReactivePowerCompensationFactorInHvrt = 42114,

    /// <remarks>
    /// Indicates whether to enable the quick startup function after the power grid recovers from a short-time interruption. If this parameter is set to 0, the function is disabled, that is, the normal startup process is still used after the power grid recovers from a short-time interruption. If this parameter is set to 1, the quick startup process is used after the power grid recovers from a short-time interruption. In that process, some detection items are skipped and the power grid is quickly connected. Whether the power grid experiences a short-time interruption can be identified depending on whether the power grid recovers within the duration for determining short-time grid disconnection.
    /// </remarks>
    [SolarDataNameMetadata(42116, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    QuickStartupForShortTimeGridDisconnection = 42116,

    [SolarDataNameMetadata(42118, FusionSolarDataType.UInt16, 1, Gain = 100)]
    [FusionSolarWrite]
    LowVoltageRideThroughActiveCurrentMaintenanceCoefficient = 42118,

    /// <remarks>
    /// By default, this function is enabled for the BDEW standard and disabled for other standards.
    /// </remarks>
    [SolarDataNameMetadata(42119, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    LowVoltageRideThrough = 42119,

    /// <remarks>
    /// Specifeis the threshold for triggering LVRT. The threshold settings should meet the local grid standard.
    /// </remarks>
    [SolarDataNameMetadata(42120, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    LowVoltageRideThroughTriggeringThreshold = 42120,

    /// <summary>Specifies whether to shield the voltage protection function during LVRT or HVRT</summary>
    [SolarDataNameMetadata(42121, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    DeactivateGridVoltageProtectionDuringVrt = 42121,

    /// <remarks>
    /// During LVRT, the device needs to generate positive- sequence reactive power to support the power grid. This parameter is used to set the positive-sequence reactive power generated by the device. For example, if Positive-sequence reactive power compensation factor in LVRT is set to 2, the positive-sequence reactive current generated by the device increases by 20% of the rated current each time the AC voltage decreases by 10% during LVRT.
    /// </remarks>
    [SolarDataNameMetadata(42122, FusionSolarDataType.UInt16, 1, Gain = 10)]
    [FusionSolarWrite]
    PositiveSequenceReactivePowerCompensationFactorInLowVoltageRideThrough = 42122,

    /// <remarks>
    /// Specifies the LVRT/HVRT recovery threshold. LVRT recovery threshold = LVRT triggering threshold + VRT exit hysteresis threshold; HVRT recovery threshold = HVRT triggering threshold – VRT exit hysteresis threshold
    /// </remarks>
    [SolarDataNameMetadata(42123, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    VrtExitHysteresisThreshold = 42123,

    [SolarDataNameMetadata(42124, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    VrtActiveCurrentLimitingInPercentage = 42124,

    [SolarDataNameMetadata(42125, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%/s")]
    [FusionSolarWrite]
    VrtActivePowerRecoveryGradient = 42125,

    [SolarDataNameMetadata(42126, FusionSolarDataType.UInt16, 1, Gain = 10)]
    [FusionSolarWrite]
    NegativeSequenceReactivePowerCompensationFactorInHvrt = 42126,

    /// <remarks>
    /// During LVRT, the device needs to generate negative- sequence reactive power to support the power grid. This parameter is used to set the negative-sequence reactive power generated by the device. For example, if Negative-sequence reactive power compensation factor in LVRT is set to 2, the negative-sequence reactive current generated by the device increases by 20% of the rated current each time the AC voltage decreases by 10% during LVRT.
    /// </remarks>
    [SolarDataNameMetadata(42127, FusionSolarDataType.UInt16, 1, Gain = 10)]
    [FusionSolarWrite]
    NegativeSequenceReactivePowerCompensationFactorInLowVoltageRideThrough = 42127,

    [SolarDataNameMetadata(42128, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PhaseAngleOffsetProtection = 42128,

    [SolarDataNameMetadata(42129, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ActiveIslandingProtection = 42129,

    [SolarDataNameMetadata(42130, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    PassiveIslandingProtection = 42130,

    [SolarDataNameMetadata(42131, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    OvgrLinkedShutdown = 42131,

    [SolarDataNameMetadata(42132, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    DryContactFunction = 42132,

    /// <remarks>
    /// During LVRT, the device needs to limit the reactive current. For example, if LVRT reactive current limiting in percentage is set to 50, the reactive current upper limit of the device is 50% of the rated current during LVRT.
    /// </remarks>
    [SolarDataNameMetadata(42133, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    LowVoltageRideThroughReactiveCurrentLimitingInPercentage = 42133,

    /// <remarks>
    /// If Zero-current mode on power grid fault is enabled and the power grid voltage is less than Threshold of LVRT zero-current mode during LVRT, the zero current mode is used. Otherwise, the mode set in LVRT mode is used.
    /// </remarks>
    [SolarDataNameMetadata(42134, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    ThresholdOfLowVoltageRideThroughZeroCurrentMode = 42134,

    [SolarDataNameMetadata(42135, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    LowVoltageRideThroughMode = 42135,

    [SolarDataNameMetadata(42138, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    VoltageRiseSuppression = 42138,

    /// <remarks>
    /// Active power derating threshold for voltage rise suppression must be greater than that Reactive power adjustment threshold for voltage rise suppression.
    /// </remarks>
    [SolarDataNameMetadata(42139, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    ReactivePowerAdjustmentThresholdForVoltageRiseSuppression = 42139,

    /// <remarks>
    /// Active power derating threshold for voltage rise suppression must be greater than that Reactive power adjustment threshold for voltage rise suppression.
    /// </remarks>
    [SolarDataNameMetadata(42140, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    ActivePowerDeratingThresholdForVoltageRiseSuppression = 42140,

    /// <remarks>
    /// According to the standards of some countries or regions, the power grid frequency may change around the rated value. In this case, the active power output needs to be slightly adjusted based on Adjustment ratio of frequency-based control to stabilize the power grid frequency. In this case, set this parameter to Enable. Frequency sensitive mode (FSM), which is described in the G99 standard.
    /// </remarks>
    [SolarDataNameMetadata(42141, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    FrequencyBasedControl = 42141,

    /// <remarks>
    /// Frequency sensitive mode (FSM) Droop, which is described in the G99 standard
    /// </remarks>
    [SolarDataNameMetadata(42142, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    AdjustmentRatioOfFrequencyBasedControl = 42142,

    /// <remarks>
    /// If this parameter is set to Enable, when the power grid frequency exceeds the frequency threshold for triggering overfrequency- caused power derating, the active power of the device is derated based on a certain gradient.
    /// </remarks>
    [SolarDataNameMetadata(42143, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    OverfrequencyCausedPowerDerating = 42143,

    /// <remarks>
    /// Specifies the frequency threshold for stopping overfrequency derating.
    /// </remarks>
    [SolarDataNameMetadata(42144, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyThresholdForStoppingOverfrequencyCausedPowerDerating = 42144,

    /// <remarks>
    /// Specifeis the power threshold for stopping overfrequency- caused derating.
    /// </remarks>
    [SolarDataNameMetadata(42145, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    PowerThresholdForStoppingOverfrequencyCausedPowerDerating = 42145,

    /// <remarks>
    /// The standards of certain countries and regions require that the output active power of devices be derated when the power grid frequency exceeds a certain value.
    /// </remarks>
    [SolarDataNameMetadata(42146, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyThresholdForTriggeringOverfrequencyCausedPowerDerating = 42146,

    /// <remarks>
    /// Specifies the frequency threshold for exiting overfrequency derating.
    /// </remarks>
    [SolarDataNameMetadata(42147, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyThresholdForExitingOverfrequencyCausedPowerDerating = 42147,

    /// <remarks>
    /// Specifies the power recovery rate for overfrequency- caused power derating.
    /// </remarks>
    [SolarDataNameMetadata(42148, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%/min")]
    [FusionSolarWrite]
    OverfrequencyCausedDeratingPowerRecoveryGradient = 42148,

    /// <remarks>
    /// The standards of certain countries and regions require that if the power grid frequency is lower than Frequency threshold for triggering underfrequency- caused power raising, the device needs to increase the active power output to increase the power grid frequency. In this case, set this parameter to Enable.
    /// </remarks>
    [SolarDataNameMetadata(42151, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    UnderfrequencyCausedPowerRaising = 42151,

    /// <remarks>
    /// Specifeis the power recovery rate for underfrequency- caused power raising.
    /// </remarks>
    [SolarDataNameMetadata(42152, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%/min")]
    [FusionSolarWrite]
    GradientOfUnderfrequencyCausedPowerRaising = 42152,

    /// <summary>Specifies the low voltage ride-through capability of the device</summary>
    /// <remarks>
    /// For details, see the Key Information Description Table. The SmartLogger and management system support batch setting and provide a separate UI for users to edit.
    /// </remarks>
    [SolarDataNameMetadata(42155, FusionSolarDataType.MultiData, 21)]
    [FusionSolarWrite]
    LowVoltageRideThroughCharacteristicCurve = 42155,

    /// <remarks>
    /// Specifies the frequency threshold for stopping underfrequency-caused power raising.
    /// </remarks>
    [SolarDataNameMetadata(42176, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyThresholdForStoppingUnderfrequencyCausedPowerRaising = 42176,

    /// <remarks>
    /// Specifies the power threshold for stopping underfrequency- caused power raising.
    /// </remarks>
    [SolarDataNameMetadata(42177, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    PowerThresholdForStoppingUnderfrequencyCausedPowerRaising = 42177,

    /// <remarks>
    /// Specifies the frequency threshold for triggering underfrequency-caused power raising.
    /// </remarks>
    [SolarDataNameMetadata(42178, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyThresholdForTriggeringUnderfrequencyCausedPowerRaising = 42178,

    /// <remarks>
    /// Specifies the frequency threshold for exiting underfrequency-caused power raising.
    /// </remarks>
    [SolarDataNameMetadata(42179, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    FrequencyThresholdForExitingUnderfrequencyCausedPowerRaising = 42179,

    [SolarDataNameMetadata(42180, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    BuiltInPidRunningMode = 42180,

    /// <remarks>
    /// Fixed output. This interface is reserved. The UI is not open.
    /// </remarks>
    [SolarDataNameMetadata(42181, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    PidOutputVoltage = 42181,

    /// <remarks>
    /// Currently, this parameter is reserved only for testing.
    /// </remarks>
    [SolarDataNameMetadata(42182, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Pid = 42182,

    /// <remarks>
    /// This is a broadcast interface dedicated for the SmartLogger and does not support incremental reporting. It is used in remote output scenarios in Japan.
    /// </remarks>
    [SolarDataNameMetadata(42192, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "%/s")]
    [FusionSolarWrite]
    BroadcastActivePowerChangeGradient = 42192,

    [SolarDataNameMetadata(42221, FusionSolarDataType.MultiData, 21)]
    [FusionSolarWrite]
    PUCurve = 42221,

    [SolarDataNameMetadata(42242, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "s")]
    [FusionSolarWrite]
    PUCurveAdjustmentTime = 42242,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42290, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Value10MinuteOvervoltageProtectionThreshold = 42290,

    [SolarDataNameMetadata(42291, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Value10MinuteOvervoltageProtectionDuration = 42291,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42293, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level1OvervoltageProtectionThreshold = 42293,

    [SolarDataNameMetadata(42294, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level1OvervoltageProtectionDuration = 42294,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42296, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level2OvervoltageProtectionThreshold = 42296,

    [SolarDataNameMetadata(42297, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level2OvervoltageProtectionDuration = 42297,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42299, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level3OvervoltageProtectionThreshold = 42299,

    [SolarDataNameMetadata(42300, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level3OvervoltageProtectionDuration = 42300,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42302, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level4OvervoltageProtectionThreshold = 42302,

    [SolarDataNameMetadata(42303, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level4OvervoltageProtectionDuration = 42303,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42305, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level5OvervoltageProtectionThreshold = 42305,

    [SolarDataNameMetadata(42306, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level5OvervoltageProtectionDuration = 42306,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42308, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level6OvervoltageProtectionThreshold = 42308,

    [SolarDataNameMetadata(42309, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level6OvervoltageProtectionDuration = 42309,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42311, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level1UndervoltageProtectionThreshold = 42311,

    [SolarDataNameMetadata(42312, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level1UndervoltageProtectionDuration = 42312,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42314, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level2UndervoltageProtectionThreshold = 42314,

    [SolarDataNameMetadata(42315, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level2UndervoltageProtectionDuration = 42315,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42317, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level3UndervoltageProtectionThreshold = 42317,

    [SolarDataNameMetadata(42318, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level3UndervoltageProtectionDuration = 42318,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42320, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level4UndervoltageProtectionThreshold = 42320,

    [SolarDataNameMetadata(42321, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level4UndervoltageProtectionDuration = 42321,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42323, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level5UndervoltageProtectionThreshold = 42323,

    [SolarDataNameMetadata(42324, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level5UndervoltageProtectionDuration = 42324,

    /// <remarks>
    /// Vn: voltage level, which is related to the grid code
    /// </remarks>
    [SolarDataNameMetadata(42326, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    [FusionSolarWrite]
    Level6UndervoltageProtectionThreshold = 42326,

    [SolarDataNameMetadata(42327, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level6UndervoltageProtectionDuration = 42327,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42329, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level1OverfrequencyProtectionThreshold = 42329,

    [SolarDataNameMetadata(42330, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level1OverfrequencyProtectionDuration = 42330,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42332, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level2OverfrequencyProtectionThreshold = 42332,

    [SolarDataNameMetadata(42333, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level2OverfrequencyProtectionDuration = 42333,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42335, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level3OverfrequencyProtectionThreshold = 42335,

    [SolarDataNameMetadata(42336, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level3OverfrequencyProtectionDuration = 42336,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42338, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level4OverfrequencyProtectionThreshold = 42338,

    [SolarDataNameMetadata(42339, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level4OverfrequencyProtectionDuration = 42339,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42341, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level5OverfrequencyProtectionThreshold = 42341,

    [SolarDataNameMetadata(42342, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level5OverfrequencyProtectionDuration = 42342,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42344, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level6OverfrequencyProtectionThreshold = 42344,

    [SolarDataNameMetadata(42345, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level6OverfrequencyProtectionDuration = 42345,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42347, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level1UnderfrequencyProtectionThreshold = 42347,

    [SolarDataNameMetadata(42348, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level1UnderfrequencyProtectionDuration = 42348,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42350, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level2UnderfrequencyProtectionThreshold = 42350,

    [SolarDataNameMetadata(42351, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level2UnderfrequencyProtectionDuration = 42351,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42353, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level3UnderfrequencyProtectionThreshold = 42353,

    [SolarDataNameMetadata(42354, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level3UnderfrequencyProtectionDuration = 42354,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42356, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level4UnderfrequencyProtectionThreshold = 42356,

    [SolarDataNameMetadata(42357, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level4UnderfrequencyProtectionDuration = 42357,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42359, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level5UnderfrequencyProtectionThreshold = 42359,

    [SolarDataNameMetadata(42360, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level5UnderfrequencyProtectionDuration = 42360,

    /// <remarks>
    /// Fn: indicates the frequency level, which is related to the grid code.
    /// </remarks>
    [SolarDataNameMetadata(42362, FusionSolarDataType.UInt16, 1, Gain = 100, Unit = "Hz")]
    [FusionSolarWrite]
    Level6UnderfrequencyProtectionThreshold = 42362,

    [SolarDataNameMetadata(42363, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "ms")]
    [FusionSolarWrite]
    Level6UnderfrequencyProtectionDuration = 42363,

    [SolarDataNameMetadata(42590, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    DelayedUpgrade = 42590,

    [SolarDataNameMetadata(42591, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    HibernateAtNight = 42591,

    [SolarDataNameMetadata(42594, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    SmartStringMonitoring = 42594,

    /// <remarks>
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(42595, FusionSolarDataType.UInt16, 1, Gain = 100)]
    [FusionSolarWrite]
    StringDetectionReferenceAsymmetricCoefficient = 42595,

    [SolarDataNameMetadata(42596, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "%")]
    [FusionSolarWrite]
    StringDetectionStartupPowerPercentage = 42596,

    [SolarDataNameMetadata(42597, FusionSolarDataType.Int16, 1, Gain = 1, Unit = "min")]
    [FusionSolarWrite]
    CommunicationInterruptionDuration = 42597,

    /// <remarks>
    /// Broadcast command interface. Note: The command value range is extended here. The most signifciant eight bits are used to mask the specified inspection action in the inspection function. This feature improves the inspection efficiency for specific purposes. Shen Yanbai 2019-07-02
    /// </remarks>
    [SolarDataNameMetadata(42730, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    Inspection = 42730,

    /// <remarks>
    /// Broadcast command interface
    /// </remarks>
    [SolarDataNameMetadata(42779, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    IVCurveScanning = 42779,

    /// <remarks>
    /// This parameter is left empty by default and is used by customers to change the device name.
    /// </remarks>
    [SolarDataNameMetadata(43349, FusionSolarDataType.String, 10)]
    [FusionSolarWrite]
    DeviceName = 43349,

    [SolarDataNameMetadata(45000, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    RestoreFactorySettings = 45000,

    [SolarDataNameMetadata(45001, FusionSolarDataType.MultiData, 2)]
    [FusionSolarWriteOnly]
    ClearActiveAlarms = 45001,

    [SolarDataNameMetadata(45003, FusionSolarDataType.MultiData, 2)]
    [FusionSolarWriteOnly]
    ResetAlarms = 45003,

    [SolarDataNameMetadata(45005, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    ClearAlarms = 45005,

    [SolarDataNameMetadata(45006, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    MaskAlarms = 45006,

    /// <remarks>
    /// Set the data field to 0. It is associated with the AFCI controller in the subdevice presence flag.
    /// </remarks>
    [SolarDataNameMetadata(45007, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    StartAfciCheck = 45007,

    [SolarDataNameMetadata(45008, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    [FusionSolarWriteOnly]
    AdjustTotalEnergyYield = 45008,

    /// <remarks>
    /// Includes the total energy generated and total energy charged on the power side.
    /// </remarks>
    [SolarDataNameMetadata(45010, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    ClearHistoricalYield = 45010,

    /// <summary>Clears information such as the total runtime, total fault duration, and total grid-tied runtime</summary>
    [SolarDataNameMetadata(45011, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    ClearRuntimeInfo = 45011,

    [SolarDataNameMetadata(45012, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    SpotCheck = 45012,

    [SolarDataNameMetadata(45015, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    EsnApplicationCommand = 45015,

    /// <remarks>
    /// Enter the device ESN in the data field.
    /// </remarks>
    [SolarDataNameMetadata(45016, FusionSolarDataType.MultiData, 10, Gain = 1)]
    [FusionSolarWriteOnly]
    InformationRestore = 45016,

    [SolarDataNameMetadata(45223, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    ClearingUserData = 45223,

    [SolarDataNameMetadata(45235, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "%")]
    [FusionSolarWrite]
    PhaseAActivePowerPercentage = 45235,

    [SolarDataNameMetadata(45236, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "%")]
    [FusionSolarWrite]
    PhaseBActivePowerPercentage = 45236,

    [SolarDataNameMetadata(45237, FusionSolarDataType.Int16, 1, Gain = 100, Unit = "%")]
    [FusionSolarWrite]
    PhaseCActivePowerPercentage = 45237,

    [SolarDataNameMetadata(45238, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ThreePhaseImbalanceControl = 45238,

    [SolarDataNameMetadata(37926, FusionSolarDataType.UInt16, 1, Gain = 1)]
    SohCalibrationStatus = 37926,

    [SolarDataNameMetadata(37927, FusionSolarDataType.UInt16, 1, Gain = 10)]
    SohCalibrationReleaseTheLowerDischargeLimitOfTheStateOfCapacity = 37927,

    [SolarDataNameMetadata(37928, FusionSolarDataType.UInt16, 1, Gain = 10)]
    SohCalibrationEnableTheBackupPowerStateOfCapacity = 37928,

    /// <remarks>
    /// 0: disabled
    /// 1: enabled
    /// </remarks>
    [SolarDataNameMetadata(47112, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWriteOnly]
    EnergyStoragePreheating = 47112,

    /// <remarks>
    /// &gt; 0: charging;
    /// &lt; 0: discharging;
    /// 0: not charging or discharging
    /// </remarks>
    [SolarDataNameMetadata(47321, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    [FusionSolarWrite]
    ThreePartySchedulingEnergyStorageChargeAndDischargePower = 47321,

    /// <remarks>
    /// 0: Unlimited (default)
    /// 1: DI active scheduling
    /// 5: Zero power grid connection
    /// 6: Power- limited grid connection (kW)
    /// 7: Power- limited grid connection (%)
    /// </remarks>
    [SolarDataNameMetadata(47415, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    ActivePowerControlMode = 47415,

    /// <remarks>
    /// [-1000,Plant Capacity] default:0
    /// Review: Unit spelling normalized from Kw.
    /// </remarks>
    [SolarDataNameMetadata(47416, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    MaximumFeedGridPowerKW = 47416,

    /// <remarks>
    /// [0,100] default:0
    /// </remarks>
    [SolarDataNameMetadata(47418, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    MaximumFeedGridPowerPercent = 47418,

    /// <remarks>
    /// 0: near-end control
    /// 1: remote control- maximum self- consumption
    /// 2: remote control-full Internet access
    /// 3: remote control--TOU
    /// 4: Remote control- intelligent energy management
    /// 5: remote control-three- party scheduling
    /// </remarks>
    [SolarDataNameMetadata(47589, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ChargingAndDischargingRemoteControlMode = 47589,

    /// <remarks>
    /// Reported through IEC 104, merged PCS running status
    /// </remarks>
    [SolarDataNameMetadata(32000, FusionSolarDataType.Bitfield16, 1)]
    Teleindication_SignalNodeRemoteCommunication = 32000,

    [SolarDataNameMetadata(32002, FusionSolarDataType.Bitfield16, 1)]
    Teleindication_RunningStatusMonitorProcessing = 32002,

    [SolarDataNameMetadata(32003, FusionSolarDataType.Bitfield32, 2, Gain = 1)]
    Teleindication_RunningStatusPowerProcessing = 32003,

    [SolarDataNameMetadata(32008, FusionSolarDataType.Bitfield16, 1)]
    Teleindication_Alarm1 = 32008,

    [SolarDataNameMetadata(32009, FusionSolarDataType.Bitfield16, 1)]
    Teleindication_Alarm2 = 32009,

    [SolarDataNameMetadata(32010, FusionSolarDataType.Bitfield16, 1)]
    Teleindication_Alarm3 = 32010,

    [SolarDataNameMetadata(32011, FusionSolarDataType.Bitfield16, 1)]
    Teleindication_Alarm4 = 32011,

    /// <remarks>
    /// GroupID: 0x0000
    /// </remarks>
    [SolarDataNameMetadata(32255, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm1 = 32255,

    /// <remarks>
    /// GroupID: 0x0001
    /// </remarks>
    [SolarDataNameMetadata(32256, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm2 = 32256,

    /// <remarks>
    /// GroupID: 0x0002
    /// </remarks>
    [SolarDataNameMetadata(32257, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm3 = 32257,

    /// <remarks>
    /// GroupID: 0x0003
    /// </remarks>
    [SolarDataNameMetadata(32258, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm4 = 32258,

    /// <remarks>
    /// GroupID: 0x0004
    /// </remarks>
    [SolarDataNameMetadata(32259, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm5 = 32259,

    /// <remarks>
    /// GroupID: 0x0005
    /// </remarks>
    [SolarDataNameMetadata(32260, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm6 = 32260,

    /// <remarks>
    /// GroupID: 0x0006
    /// </remarks>
    [SolarDataNameMetadata(32261, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm7 = 32261,

    /// <remarks>
    /// GroupID: 0x0007
    /// </remarks>
    [SolarDataNameMetadata(32262, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm8 = 32262,

    /// <remarks>
    /// GroupID: 0x0008
    /// </remarks>
    [SolarDataNameMetadata(32263, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm9 = 32263,

    /// <remarks>
    /// GroupID: 0x0009
    /// </remarks>
    [SolarDataNameMetadata(32264, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm10 = 32264,

    /// <remarks>
    /// GroupID: 0x000A
    /// </remarks>
    [SolarDataNameMetadata(32265, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm11 = 32265,

    /// <remarks>
    /// GroupID: 0x000B
    /// </remarks>
    [SolarDataNameMetadata(32266, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm12 = 32266,

    /// <remarks>
    /// GroupID: 0x000C
    /// </remarks>
    [SolarDataNameMetadata(32267, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm13 = 32267,

    /// <remarks>
    /// GroupID: 0x000E
    /// </remarks>
    [SolarDataNameMetadata(32269, FusionSolarDataType.Bitfield16, 1)]
    External_PowerAlarm15 = 32269,

    /// <remarks>
    /// Change requirement: The "Reactive power adjustment time" broadcast interface should be added to the delivery UI of the Q-P characteristic curve and cosphi-P characteristic curve, which is the same as the Q-U characteristic curve 20190918.
    /// </remarks>
    [SolarDataNameMetadata(40196, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "s")]
    [FusionSolarWrite]
    CharacteristicCurve_ReactivePowerAdjustmentTime = 40196,

    [SolarDataNameMetadata(43000, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    SystemTime_Year = 43000,

    [SolarDataNameMetadata(43001, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    SystemTime_Month = 43001,

    [SolarDataNameMetadata(43002, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    SystemTime_Day = 43002,

    [SolarDataNameMetadata(43003, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    SystemTime_Hour = 43003,

    [SolarDataNameMetadata(43004, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    SystemTime_Minute = 43004,

    [SolarDataNameMetadata(43005, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWriteOnly]
    SystemTime_Second = 43005,

    [SolarDataNameMetadata(43018, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4851_ProtocolType = 43018,

    /// <remarks>
    /// 0: broadcast address;
    /// 1–247: device address;
    /// 248–255: reserved
    /// </remarks>
    [SolarDataNameMetadata(43019, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    Rs4851_CommunicationsAddress = 43019,

    /// <remarks>
    /// The baud rate 115200 corresponds to bit 26 of feature code 3.
    /// </remarks>
    [SolarDataNameMetadata(43020, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4851_BaudRate = 43020,

    [SolarDataNameMetadata(43021, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4851_VerificationMode = 43021,

    [SolarDataNameMetadata(43022, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4851_PortMode = 43022,

    [SolarDataNameMetadata(43033, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4852_ProtocolType = 43033,

    /// <remarks>
    /// 0: broadcast address;
    /// 1–247: device address;
    /// 248–255: reserved
    /// </remarks>
    [SolarDataNameMetadata(43034, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    Rs4852_CommunicationsAddress = 43034,

    /// <remarks>
    /// The baud rate 115200 corresponds to bit 27 of feature code 3.
    /// </remarks>
    [SolarDataNameMetadata(43035, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4852_BaudRate = 43035,

    [SolarDataNameMetadata(43036, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4852_VerificationMode = 43036,

    [SolarDataNameMetadata(43037, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    Rs4852_PortMode = 43037,

    /// <remarks>
    /// Is the power-on flag needs to be cleared.
    /// </remarks>
    [SolarDataNameMetadata(43359, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    App_FirstPowerOnFlag = 43359,

    /// <remarks>
    /// 0: offline
    /// 1: standby
    /// 2: running
    /// 3: fault
    /// 4: sleep mode
    /// </remarks>
    [SolarDataNameMetadata(37000, FusionSolarDataType.UInt16, 1, Gain = 1)]
    EnergyStorageUnit1_RunningStatus = 37000,

    /// <remarks>
    /// &gt; 0: charging
    /// &lt; 0: discharging
    /// </remarks>
    [SolarDataNameMetadata(37001, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    EnergyStorageUnit1_ChargeAndDischargePower = 37001,

    /// <summary>Energy storage module output voltage</summary>
    [SolarDataNameMetadata(37003, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit1_BusVoltage = 37003,

    /// <summary>Battery's state of capacity</summary>
    [SolarDataNameMetadata(37004, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit1_BatteryStateOfCapacity = 37004,

    /// <remarks>
    /// 0:none
    /// 1:Forcible charge/ discharge
    /// 2:Time of Use
    /// 3:Fixed charge/ discharge
    /// 4:Maximise self consumption
    /// 5:Fully fed to grid
    /// 6:Time of Use(LUNA2000 )
    /// 7: remote scheduling- maximum self- use
    /// 8: remote scheduling - full Internet access
    /// 9: remote scheduling - TOU
    /// 10: AI energy management and scheduling
    /// 11: Remote scheduling - AI energy management scheduling
    /// 12: third-party scheduling
    /// </remarks>
    [SolarDataNameMetadata(37006, FusionSolarDataType.UInt16, 1, Gain = 1)]
    EnergyStorageUnit1_WorkingMode = 37006,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37007, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    EnergyStorageUnit1_RatedChargePower = 37007,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37009, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    EnergyStorageUnit1_RatedDischargePower = 37009,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37014, FusionSolarDataType.UInt16, 1, Gain = 1)]
    EnergyStorageUnit1_FaultIdentifier = 37014,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37015, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_CurrentDayChargeCapacity = 37015,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37017, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_CurrentDayDischargeCapacity = 37017,

    /// <summary>Energy storage module output current</summary>
    [SolarDataNameMetadata(37021, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit1_BusCurrent = 37021,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(37022, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryTemperature = 37022,

    /// <remarks>
    /// [0,1440]
    /// Review: Unit spelling normalized from mins.
    /// </remarks>
    [SolarDataNameMetadata(37025, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "min")]
    EnergyStorageUnit1_RemainingChargeDischargeTime = 37025,

    /// <remarks>
    /// eg. 7kW-V1.3
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(37026, FusionSolarDataType.String, 10, Gain = 1)]
    EnergyStorageUnit1_DcdcVersion = 37026,

    /// <remarks>
    /// eg. 001. 002. 003. 004
    /// Review: Signal wrapping/subscript normalized; see raw PDF cells.
    /// </remarks>
    [SolarDataNameMetadata(37036, FusionSolarDataType.String, 10, Gain = 1)]
    EnergyStorageUnit1_BmsVersion = 37036,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37052, FusionSolarDataType.String, 10, Gain = 1)]
    EnergyStorageUnit1_SerialNumber = 37052,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37066, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_TotalCharge = 37066,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37068, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_TotalDischarge = 37068,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37814, FusionSolarDataType.String, 15, Gain = 1)]
    EnergyStorageUnit1_SoftwareVersion = 37814,

    /// <remarks>
    /// 0：None
    /// 1: LG-RESU
    /// 2：HUAWEI- LUNA2000
    /// </remarks>
    [SolarDataNameMetadata(47000, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorageUnit1_ProductMode = 47000,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47107, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorageUnit1_No = 47107,

    [SolarDataNameMetadata(37920, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit1_BatteryPack1_SohCalibrationStatus = 37920,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38200, FusionSolarDataType.String, 10)]
    EnergyStorageUnit1_BatteryPack1_SerialNumber = 38200,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38210, FusionSolarDataType.String, 15)]
    EnergyStorageUnit1_BatteryPack1_FirmwareVersion = 38210,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38228, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit1_BatteryPack1_WorkingStatus = 38228,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38229, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit1_BatteryPack1_StateOfCapacity = 38229,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38233, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    EnergyStorageUnit1_BatteryPack1_ChargeDischargePower = 38233,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38235, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit1_BatteryPack1_Voltage = 38235,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38236, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit1_BatteryPack1_Current = 38236,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38238, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_BatteryPack1_TotalCharge = 38238,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38240, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_BatteryPack1_TotalDischarge = 38240,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38452, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryPack1_MaximumTemperature = 38452,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38453, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryPack1_MinimumTemperature = 38453,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47750, FusionSolarDataType.UInt16, 1)]
    [FusionSolarWrite]
    EnergyStorageUnit1_BatteryPack1_No = 47750,

    [SolarDataNameMetadata(37921, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit1_BatteryPack2_SohCalibrationStatus = 37921,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38242, FusionSolarDataType.String, 10)]
    EnergyStorageUnit1_BatteryPack2_SerialNumber = 38242,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38252, FusionSolarDataType.String, 15)]
    EnergyStorageUnit1_BatteryPack2_FirmwareVersion = 38252,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38270, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit1_BatteryPack2_WorkingStatus = 38270,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38271, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit1_BatteryPack2_StateOfCapacity = 38271,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38275, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    EnergyStorageUnit1_BatteryPack2_ChargeDischargePower = 38275,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38277, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit1_BatteryPack2_Voltage = 38277,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38278, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit1_BatteryPack2_Current = 38278,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38280, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_BatteryPack2_TotalCharge = 38280,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38282, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_BatteryPack2_TotalDischarge = 38282,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38454, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryPack2_MaximumTemperature = 38454,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38455, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryPack2_MinimumTemperature = 38455,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47751, FusionSolarDataType.UInt16, 1)]
    [FusionSolarWrite]
    EnergyStorageUnit1_BatteryPack2_No = 47751,

    [SolarDataNameMetadata(37922, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit1_BatteryPack3_SohCalibrationStatus = 37922,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38284, FusionSolarDataType.String, 10)]
    EnergyStorageUnit1_BatteryPack3_SerialNumber = 38284,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38294, FusionSolarDataType.String, 15)]
    EnergyStorageUnit1_BatteryPack3_FirmwareVersion = 38294,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38312, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit1_BatteryPack3_WorkingStatus = 38312,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38313, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit1_BatteryPack3_StateOfCapacity = 38313,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38317, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    EnergyStorageUnit1_BatteryPack3_ChargeDischargePower = 38317,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38319, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit1_BatteryPack3_Voltage = 38319,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38320, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit1_BatteryPack3_Current = 38320,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38322, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_BatteryPack3_TotalCharge = 38322,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38324, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit1_BatteryPack3_TotalDischarge = 38324,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38456, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryPack3_MaximumTemperature = 38456,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38457, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit1_BatteryPack3_MinimumTemperature = 38457,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47752, FusionSolarDataType.UInt16, 1)]
    [FusionSolarWrite]
    EnergyStorageUnit1_BatteryPack3_No = 47752,

    /// <remarks>
    /// The value is the smaller value of the sum of the maximum charge and discharge capability of the solar inverter and the charge and discharge capability of the connected energy storage unit (ESU).
    /// </remarks>
    [SolarDataNameMetadata(37046, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    EnergyStorage_MaximumChargePower = 37046,

    /// <remarks>
    /// The value is the smaller value of the sum of the maximum charge and discharge capability of the solar inverter and the charge and discharge capability of the connected ESU.
    /// </remarks>
    [SolarDataNameMetadata(37048, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    EnergyStorage_MaximumDischargePower = 37048,

    [SolarDataNameMetadata(37758, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "Wh")]
    EnergyStorage_RatedCapacity = 37758,

    /// <remarks>
    /// [0.0,100.0]
    /// </remarks>
    [SolarDataNameMetadata(37760, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorage_StateOfCapacity = 37760,

    /// <remarks>
    /// 0: offline
    /// 1: standby
    /// 2: running
    /// 3: fault
    /// 4: sleep mode
    /// </remarks>
    [SolarDataNameMetadata(37762, FusionSolarDataType.UInt16, 1, Gain = 1)]
    EnergyStorage_RunningStatus = 37762,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37763, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorage_BusVoltage = 37763,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37764, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorage_BusCurrent = 37764,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37765, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    EnergyStorage_ChargeDischargePower = 37765,

    /// <remarks>
    /// Life-cycle accumulation
    /// </remarks>
    [SolarDataNameMetadata(37780, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorage_TotalCharge = 37780,

    /// <remarks>
    /// Life-cycle accumulation
    /// </remarks>
    [SolarDataNameMetadata(37782, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorage_TotalDischarge = 37782,

    /// <remarks>
    /// Current-day accumulation
    /// </remarks>
    [SolarDataNameMetadata(37784, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorage_CurrentDayChargeCapacity = 37784,

    /// <remarks>
    /// Current-day accumulation
    /// </remarks>
    [SolarDataNameMetadata(37786, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorage_CurrentDayDischargeCapacity = 37786,

    /// <remarks>
    /// For details, see5.4
    /// </remarks>
    [SolarDataNameMetadata(47028, FusionSolarDataType.MultiData, 41, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_TimeOfUseElectricityPricePeriods = 47028,

    /// <remarks>
    /// [0, Upper threshold] Default value: 3500;
    /// </remarks>
    [SolarDataNameMetadata(47075, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    [FusionSolarWrite]
    EnergyStorage_MaximumChargingPower = 47075,

    /// <remarks>
    /// [0, Upper threshold] Default value: 3500;
    /// </remarks>
    [SolarDataNameMetadata(47077, FusionSolarDataType.UInt32, 2, Gain = 1, Unit = "W")]
    [FusionSolarWrite]
    EnergyStorage_MaximumDischargingPower = 47077,

    /// <remarks>
    /// [90,100] Default value:100
    /// </remarks>
    [SolarDataNameMetadata(47081, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    EnergyStorage_ChargingCutoffCapacity = 47081,

    /// <remarks>
    /// [12,20] Default value:15
    /// </remarks>
    [SolarDataNameMetadata(47082, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    EnergyStorage_DischargeCutoffCapacity = 47082,

    /// <remarks>
    /// [0,1440] The value is not stored.
    /// Review: Unit spelling normalized from mins.
    /// </remarks>
    [SolarDataNameMetadata(47083, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "min")]
    [FusionSolarWrite]
    EnergyStorage_ForcedChargingAndDischargingPeriod = 47083,

    /// <remarks>
    /// [- "Maximum discharge power", "Maximum charge power"] Forced charge/ discharge power ≤ Maximum charge/ discharge power ≤ Rated charge/ discharge power Do not store
    /// </remarks>
    [SolarDataNameMetadata(47084, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    [FusionSolarWrite]
    EnergyStorage_ForcedChargingAndDischargingPower = 47084,

    /// <remarks>
    /// 0:Adaptive(Fixe d charge/ discharge/ Maximise self consumption) ；
    /// 1:Fixed charge/ discharge；
    /// 2:Maximise self consumption;
    /// 3:Time Of Use；
    /// 4:Fully fed to grid；
    /// 5:Time Of Use(Luna)
    /// </remarks>
    [SolarDataNameMetadata(47086, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_WorkingModeSettings = 47086,

    /// <remarks>
    /// 0: Disable
    /// 1: Enable
    /// </remarks>
    [SolarDataNameMetadata(47087, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_ChargeFromGridFunction = 47087,

    /// <remarks>
    /// [20,100] Default: 50
    /// </remarks>
    [SolarDataNameMetadata(47088, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    EnergyStorage_GridChargeCutoffStateOfCapacity = 47088,

    /// <remarks>
    /// 0: Stop
    /// 1: Charge
    /// 2: Discharge
    /// </remarks>
    [SolarDataNameMetadata(47100, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_ForcibleChargeDischarge = 47100,

    /// <remarks>
    /// [0,100]
    /// </remarks>
    [SolarDataNameMetadata(47101, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    EnergyStorage_TargetStateOfCapacity = 47101,

    /// <remarks>
    /// Definition: Reseved energy storage capacity in backup mode Setting range: [0,100] Execution range: Luna: [0,100], Default value: 0
    /// </remarks>
    [SolarDataNameMetadata(47102, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    [FusionSolarWrite]
    EnergyStorage_BackupPowerStateOfCapacity = 47102,

    /// <remarks>
    /// 0:none
    /// 1:LG-RESU
    /// 2:HUAWEI- LUNA2000
    /// </remarks>
    [SolarDataNameMetadata(47106, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_ProductModel = 47106,

    /// <remarks>
    /// For details, see5.4
    /// </remarks>
    [SolarDataNameMetadata(47200, FusionSolarDataType.MultiData, 41, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_FixedChargingAndDischargingPeriods = 47200,

    /// <remarks>
    /// [0，maximum power of charge from grid]
    /// </remarks>
    [SolarDataNameMetadata(47242, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    EnergyStorage_PowerOfChargeFromGrid = 47242,

    /// <remarks>
    /// Default:2000
    /// </remarks>
    [SolarDataNameMetadata(47244, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    EnergyStorage_MaximumPowerOfChargeFromGrid = 47244,

    /// <remarks>
    /// 0：Duration
    /// </remarks>
    [SolarDataNameMetadata(47246, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_ForcibleChargeDischargeSettingMode = 47246,

    /// <remarks>
    /// [0,maximum charge power]
    /// </remarks>
    [SolarDataNameMetadata(47247, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    EnergyStorage_ForcibleChargePower = 47247,

    /// <remarks>
    /// [0,maximum discharge power]
    /// </remarks>
    [SolarDataNameMetadata(47249, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    EnergyStorage_ForcibleDischargePower = 47249,

    /// <remarks>
    /// For details, see5.4
    /// Section 5.4 does not identify this exact 43-word structure; do not assume the 41-word examples apply.
    /// </remarks>
    [SolarDataNameMetadata(47255, FusionSolarDataType.Bytes, 43, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_TimeOfUseChargingAndDischargingPeriods = 47255,

    /// <remarks>
    /// 0: Fed to grid
    /// 1: Charge
    /// Note: Bound with the grid code. Japan: Fed to grid Other countries: Charge
    /// </remarks>
    [SolarDataNameMetadata(47299, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorage_ExcessPvEnergyUseInTimeOfUse = 47299,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37700, FusionSolarDataType.Bytes, 10, Gain = 1)]
    EnergyStorageUnit2_SerialNumber = 37700,

    /// <summary>Battery's state of capacity</summary>
    [SolarDataNameMetadata(37738, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit2_BatteryStateOfCapacity = 37738,

    /// <remarks>
    /// 0: offline
    /// 1: standby
    /// 2: running
    /// 3: fault
    /// 4: sleep mode
    /// </remarks>
    [SolarDataNameMetadata(37741, FusionSolarDataType.UInt16, 1, Gain = 1)]
    EnergyStorageUnit2_RunningStatus = 37741,

    /// <remarks>
    /// &gt; 0: charging
    /// &lt; 0: discharging
    /// </remarks>
    [SolarDataNameMetadata(37743, FusionSolarDataType.Int32, 2, Gain = 1, Unit = "W")]
    EnergyStorageUnit2_ChargeAndDischargePower = 37743,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37746, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_CurrentDayChargeCapacity = 37746,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37748, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_CurrentDayDischargeCapacity = 37748,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37750, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit2_BusVoltage = 37750,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37751, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit2_BusCurrent = 37751,

    /// <remarks>
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(37752, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryTemperature = 37752,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37753, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_TotalCharge = 37753,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37755, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_TotalDischarge = 37755,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(37799, FusionSolarDataType.String, 15, Gain = 1)]
    EnergyStorageUnit2_SoftwareVersion = 37799,

    /// <remarks>
    /// 0: none
    /// 1: LG-RESU
    /// 2: HUAWEI- LUNA2000
    /// </remarks>
    [SolarDataNameMetadata(47089, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorageUnit2_ProductModel = 47089,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47108, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    EnergyStorageUnit2_No = 47108,

    [SolarDataNameMetadata(37923, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit2_BatteryPack1_SohCalibrationStatus = 37923,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38326, FusionSolarDataType.String, 10)]
    EnergyStorageUnit2_BatteryPack1_SerialNumber = 38326,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38336, FusionSolarDataType.String, 15)]
    EnergyStorageUnit2_BatteryPack1_FirmwareVersion = 38336,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38354, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit2_BatteryPack1_WorkingStatus = 38354,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38355, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit2_BatteryPack1_StateOfCapacity = 38355,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38359, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    EnergyStorageUnit2_BatteryPack1_ChargeDischargePower = 38359,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38361, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit2_BatteryPack1_Voltage = 38361,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38362, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit2_BatteryPack1_Current = 38362,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38364, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_BatteryPack1_TotalCharge = 38364,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38366, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_BatteryPack1_TotalDischarge = 38366,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38458, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryPack1_MaximumTemperature = 38458,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38459, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryPack1_MinimumTemperature = 38459,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47753, FusionSolarDataType.UInt16, 1)]
    [FusionSolarWrite]
    EnergyStorageUnit2_BatteryPack1_No = 47753,

    [SolarDataNameMetadata(37924, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit2_BatteryPack2_SohCalibrationStatus = 37924,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38368, FusionSolarDataType.String, 10)]
    EnergyStorageUnit2_BatteryPack2_SerialNumber = 38368,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38378, FusionSolarDataType.String, 15)]
    EnergyStorageUnit2_BatteryPack2_FirmwareVersion = 38378,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38396, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit2_BatteryPack2_WorkingStatus = 38396,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38397, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit2_BatteryPack2_StateOfCapacity = 38397,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38401, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    EnergyStorageUnit2_BatteryPack2_ChargeDischargePower = 38401,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38403, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit2_BatteryPack2_Voltage = 38403,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38404, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit2_BatteryPack2_Current = 38404,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38406, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_BatteryPack2_TotalCharge = 38406,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38408, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_BatteryPack2_TotalDischarge = 38408,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38460, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryPack2_MaximumTemperature = 38460,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38461, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryPack2_MinimumTemperature = 38461,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47754, FusionSolarDataType.UInt16, 1)]
    [FusionSolarWrite]
    EnergyStorageUnit2_BatteryPack2_No = 47754,

    [SolarDataNameMetadata(37925, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit2_BatteryPack3_SohCalibrationStatus = 37925,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38410, FusionSolarDataType.String, 10)]
    EnergyStorageUnit2_BatteryPack3_SerialNumber = 38410,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38420, FusionSolarDataType.String, 15)]
    EnergyStorageUnit2_BatteryPack3_FirmwareVersion = 38420,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38438, FusionSolarDataType.UInt16, 1)]
    EnergyStorageUnit2_BatteryPack3_WorkingStatus = 38438,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38439, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "%")]
    EnergyStorageUnit2_BatteryPack3_StateOfCapacity = 38439,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38443, FusionSolarDataType.Int32, 2, Gain = 1000, Unit = "kW")]
    EnergyStorageUnit2_BatteryPack3_ChargeDischargePower = 38443,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38445, FusionSolarDataType.UInt16, 1, Gain = 10, Unit = "V")]
    EnergyStorageUnit2_BatteryPack3_Voltage = 38445,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38446, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "A")]
    EnergyStorageUnit2_BatteryPack3_Current = 38446,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38448, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_BatteryPack3_TotalCharge = 38448,

    /// <remarks>
    /// -
    /// </remarks>
    [SolarDataNameMetadata(38450, FusionSolarDataType.UInt32, 2, Gain = 100, Unit = "kWh")]
    EnergyStorageUnit2_BatteryPack3_TotalDischarge = 38450,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38462, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryPack3_MaximumTemperature = 38462,

    /// <remarks>
    /// -
    /// Review: Unit spelling normalized from ℃.
    /// </remarks>
    [SolarDataNameMetadata(38463, FusionSolarDataType.Int16, 1, Gain = 10, Unit = "°C")]
    EnergyStorageUnit2_BatteryPack3_MinimumTemperature = 38463,

    /// <remarks>
    /// [0,65534] Default: 0. Which means no equipment.
    /// </remarks>
    [SolarDataNameMetadata(47755, FusionSolarDataType.UInt16, 1)]
    [FusionSolarWrite]
    EnergyStorageUnit2_BatteryPack3_No = 47755,

    /// <remarks>
    /// Review: Unit spelling normalized from KW.
    /// </remarks>
    [SolarDataNameMetadata(38904, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    ParallelSystem_RechargeablePower = 38904,

    [SolarDataNameMetadata(38906, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    ParallelSystem_DischargeablePower = 38906,

    [SolarDataNameMetadata(38908, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "W")]
    ParallelSystem_HeatingPreRequestedPower = 38908,

    [SolarDataNameMetadata(38909, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "W")]
    ParallelSystem_DehumidificationRequestPower = 38909,

    /// <remarks>
    /// Byte0: request type.
    /// 0: no request (default)
    /// 1: charging first
    /// 2: emergency charging
    /// 3: forcible charging
    /// 4: discharge first
    /// 5: forced discharge
    /// Byte1 - Byte 4 Maximum charge/ discharge power (unit: W, gain 1)
    /// </remarks>
    [SolarDataNameMetadata(38910, FusionSolarDataType.MultiDataOrBytes, 3)]
    ParallelSystem_BatteryChargeDischargeRequest = 38910,

    /// <remarks>
    /// Value range: 0 to 1440
    /// Review: PDF has a physical unit but no numeric gain.
    /// </remarks>
    [SolarDataNameMetadata(38913, FusionSolarDataType.UInt16, 1, Unit = "min")]
    ParallelSystem_EnergyStoragePreheatingNeedsTime = 38913,

    /// <remarks>
    /// 0: initial
    /// 1: start event
    /// 2: end event
    /// 3: reminder event
    /// </remarks>
    [SolarDataNameMetadata(38914, FusionSolarDataType.Enum16, 1)]
    ParallelSystem_BatteryHealthCheckEvent = 38914,

    /// <remarks>
    /// 0: to be detected
    /// 1: automatic detection
    /// 2: manual detection
    /// 3: The detection is successful.
    /// 4: check failed
    /// </remarks>
    [SolarDataNameMetadata(38915, FusionSolarDataType.Enum16, 1)]
    ParallelSystem_BatteryHealthCheckStatus = 38915,

    /// <remarks>
    /// 0: stop
    /// 1: start
    /// </remarks>
    [SolarDataNameMetadata(48219, FusionSolarDataType.Enum16, 1)]
    [FusionSolarWrite]
    ParallelSystem_ManualBatteryHealthCheck = 48219,

    /// <remarks>
    /// [0,30]
    /// Review: Unit spelling normalized from Kw.
    /// </remarks>
    [SolarDataNameMetadata(47590, FusionSolarDataType.UInt32, 2, Gain = 1000, Unit = "kW")]
    [FusionSolarWrite]
    Dongle_Plant_MaximumChargeFromGridPower = 47590,

    /// <remarks>
    /// 0：Switch from grid-tied to off-grid
    /// </remarks>
    [SolarDataNameMetadata(47604, FusionSolarDataType.UInt16, 1, Gain = 1)]
    [FusionSolarWrite]
    Backup_SwitchToOffGrid = 47604,

    /// <remarks>
    /// 0: 101V
    /// 1: 202V
    /// </remarks>
    [SolarDataNameMetadata(47605, FusionSolarDataType.UInt16, 1, Gain = 1, Unit = "V")]
    [FusionSolarWrite]
    Backup_VoltageInIndependentOperation = 47605,
}
