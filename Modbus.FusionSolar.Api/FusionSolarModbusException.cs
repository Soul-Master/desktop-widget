namespace Modbus.FusionSolar.Api;

/// <summary>A Modbus exception response returned by the device.</summary>
/// <remarks>Known codes follow SUN2000MA SPC106 section 6.3.2. Receiving an exception does not trigger an automatic retry.</remarks>
public sealed class FusionSolarModbusException(byte functionCode, byte exceptionCode)
    : IOException($"Modbus function 0x{functionCode:X2} failed with exception code 0x{exceptionCode:X2}. {GetDescription(exceptionCode)}")
{
    /// <summary>The requested function code, without the exception response's high bit.</summary>
    public byte FunctionCode { get; } = functionCode;

    /// <summary>The original device code, including codes absent from the supplied specification.</summary>
    public byte ExceptionCode { get; } = exceptionCode;

    /// <summary>The documented exception code, or null for an unknown code.</summary>
    public FusionSolarModbusExceptionCode? KnownExceptionCode { get; } =
        Enum.IsDefined((FusionSolarModbusExceptionCode)exceptionCode) ? (FusionSolarModbusExceptionCode)exceptionCode : null;

    /// <summary>A readable explanation, or an explicit unknown-code description.</summary>
    public string Description { get; } = GetDescription(exceptionCode);

    private static string GetDescription(byte code) => (FusionSolarModbusExceptionCode)code switch
    {
        FusionSolarModbusExceptionCode.IllegalFunction =>
            "Illegal function: the function is unsupported or unavailable in the device's current state.",
        FusionSolarModbusExceptionCode.IllegalDataAddress =>
            "Illegal data address: the combination of register address and transfer length is invalid.",
        FusionSolarModbusExceptionCode.IllegalDataValue =>
            "Illegal data value: the request data or structure is invalid, such as an incorrect length; this does not specifically indicate an engineering-value range violation.",
        FusionSolarModbusExceptionCode.SlaveNodeFailure =>
            "Slave node failure: the device encountered an error while performing the requested action.",
        FusionSolarModbusExceptionCode.SlaveDeviceBusy =>
            "Slave device busy: the device cannot accept the request; the caller decides whether and when to resend it.",
        FusionSolarModbusExceptionCode.NoPermission =>
            "No permission: permission authentication failed or the permission has expired.",
        _ => "Unknown exception code: this code is not defined in SUN2000MA SPC106 section 6.3.2."
    };
}
