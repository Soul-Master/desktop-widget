namespace Modbus.FusionSolar.Api;

/// <summary>Documented wire metadata. Access flags describe capability, not device authorization.</summary>
public sealed record FusionSolarRegisterDefinition(
    string Name, ushort Address, FusionSolarDataType DataType, ushort WordCount,
    int? Gain, string? Unit, bool CanRead, bool CanWrite)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || !Enum.IsDefined(DataType) || WordCount is < 1 or > 125 ||
            Address + WordCount > 65536 || Gain is <= 0 || (!CanRead && !CanWrite) || (CanWrite && WordCount > 123))
            throw new ArgumentException($"Invalid register metadata: {Name}.");
    }
}
