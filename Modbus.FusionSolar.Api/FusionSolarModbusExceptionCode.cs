namespace Modbus.FusionSolar.Api;

/// <summary>Device exception codes from SUN2000MA SPC106 section 6.3.2, Table 6-4.</summary>
/// <remarks>This list covers the supplied specification; other raw codes may still be returned by a device.</remarks>
public enum FusionSolarModbusExceptionCode : byte
{
    /// <summary>The function is unsupported or unavailable in the device's current state.</summary>
    IllegalFunction = 0x01,

    /// <summary>The combination of register address and transfer length is invalid.</summary>
    IllegalDataAddress = 0x02,

    /// <summary>The request data or structure is invalid, such as an incorrect length.</summary>
    /// <remarks>This does not specifically indicate an engineering value outside an application's expected range.</remarks>
    IllegalDataValue = 0x03,

    /// <summary>The device encountered an error while performing the requested action.</summary>
    SlaveNodeFailure = 0x04,

    /// <summary>The device cannot accept the request; the caller decides whether and when to resend it.</summary>
    SlaveDeviceBusy = 0x06,

    /// <summary>Permission authentication failed or the permission has expired.</summary>
    NoPermission = 0x80
}
