namespace Modbus.FusionSolar.Api;

/// <summary>Documented wire metadata for a named register.</summary>
public interface IFusionSolarNameMetadata
{
    ushort Address { get; }
    FusionSolarDataType DataType { get; }
    ushort WordCount { get; }
    /// <summary>Numeric divisor; zero means the document does not specify a gain.</summary>
    int Gain { get; }
    string? Unit { get; }
}
