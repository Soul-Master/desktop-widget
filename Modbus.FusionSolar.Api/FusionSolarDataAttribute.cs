namespace Modbus.FusionSolar.Api;

/// <summary>Maps a property to a named register.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FusionSolarDataAttribute : Attribute
{
    public FusionSolarDataAttribute(FusionSolarDataNames name)
    {
        Name = name;
        var metadata = FusionSolarNameMetadata.Get(name);
        Address = metadata.Address;
        DataType = metadata.DataType;
        WordCount = metadata.WordCount;
        Gain = metadata.Gain;
        Unit = metadata.Unit;
    }

    public FusionSolarDataNames? Name { get; }
    public ushort Address { get; }
    public FusionSolarDataType DataType { get; }
    public ushort WordCount { get; }
    /// <summary>Numeric divisor. Zero means no gain is specified.</summary>
    public int Gain { get; set; }
    public string? Unit { get; set; }
}
