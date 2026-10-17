namespace Modbus.FusionSolar.Api;

/// <summary>Declares wire metadata on a register enum member; construction does not register metadata.</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public sealed class SolarDataNameMetadataAttribute(
    ushort address, FusionSolarDataType dataType, ushort wordCount) : Attribute, IFusionSolarNameMetadata
{
    public ushort Address { get; } = address;
    public FusionSolarDataType DataType { get; } = dataType;
    public ushort WordCount { get; } = wordCount;
    /// <summary>Numeric divisor; zero means the document does not specify a gain.</summary>
    public int Gain { get; set; }
    public string? Unit { get; set; }
}
