using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Modbus.FusionSolar.Api;

/// <summary>Resolves and caches only requested enum members after their attributes are fully constructed.</summary>
public static class FusionSolarNameMetadata
{
    private static readonly ConcurrentDictionary<ushort, IFusionSolarNameMetadata> Cache = new();
    private static readonly ConcurrentDictionary<ushort, Lazy<IFusionSolarNameMetadata>> Pending = new();

    /// <summary>Read-only view of immutable metadata snapshots resolved so far.</summary>
    public static IReadOnlyDictionary<ushort, IFusionSolarNameMetadata> Mapping { get; } =
        new ReadOnlyDictionary<ushort, IFusionSolarNameMetadata>(Cache);

    /// <summary>Gets validated metadata for a defined register name. Undefined numeric enum casts are rejected.</summary>
    public static IFusionSolarNameMetadata Get(FusionSolarDataNames name)
    {
        var address = (ushort)name;
        if (Cache.TryGetValue(address, out var metadata)) return metadata;

        var pending = Pending.GetOrAdd(address, static address => new Lazy<IFusionSolarNameMetadata>(
            // Recheck the cache in case another initialization completed after the first lookup.
            () => Cache.GetOrAdd(address, static address => Load((FusionSolarDataNames)address)),
            LazyThreadSafetyMode.ExecutionAndPublication));
        try
        {
            return pending.Value;
        }
        finally
        {
            // Remove only this gate, preserving a newer retry if initialization failed.
            Pending.TryRemove(new KeyValuePair<ushort, Lazy<IFusionSolarNameMetadata>>(address, pending));
        }
    }

    internal static FusionSolarRegisterDefinition GetDefinition(FusionSolarDataNames name) =>
        ((Metadata)Get(name)).Definition;

    private static Metadata Load(FusionSolarDataNames name)
    {
        var fieldName = Enum.GetName(name)
            ?? throw new ArgumentException($"Unknown FusionSolar register address: {(ushort)name}.", nameof(name));
        var field = typeof(FusionSolarDataNames).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        var metadata = field.GetCustomAttribute<SolarDataNameMetadataAttribute>()
            ?? throw new ArgumentException($"Register '{fieldName}' has no wire metadata.", nameof(name));
        if (metadata.Address != (ushort)name)
            throw new ArgumentException($"Register '{fieldName}' metadata has a different address.", nameof(name));
        var write = field.IsDefined(typeof(FusionSolarWriteAttribute));
        var writeOnly = field.IsDefined(typeof(FusionSolarWriteOnlyAttribute));
        if (write && writeOnly)
            throw new ArgumentException($"Conflicting access markers on register '{fieldName}'.", nameof(name));
        var definition = new FusionSolarRegisterDefinition(fieldName, metadata.Address, metadata.DataType,
            metadata.WordCount, metadata.Gain == 0 ? null : metadata.Gain, metadata.Unit, !writeOnly, write || writeOnly);
        definition.Validate();
        return new Metadata(definition);
    }

    // Never expose the mutable attribute instance through the shared cache.
    private sealed record Metadata(FusionSolarRegisterDefinition Definition) : IFusionSolarNameMetadata
    {
        public ushort Address => Definition.Address;
        public FusionSolarDataType DataType => Definition.DataType;
        public ushort WordCount => Definition.WordCount;
        public int Gain => Definition.Gain ?? 0;
        public string? Unit => Definition.Unit;
    }
}
