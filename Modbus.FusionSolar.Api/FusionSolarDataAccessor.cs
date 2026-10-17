using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Modbus.FusionSolar.Api;

/// <summary>Writes explicitly supplied register definitions through a caller-owned client.</summary>
/// <remarks>Definitions describe wire capability, not device authorization. The client must outlive the accessor.</remarks>
public class FusionSolarDataAccessor
{
    private readonly IReadOnlyDictionary<ushort, FusionSolarRegisterDefinition> registers;
    private protected FusionSolarClient Client { get; }

    public FusionSolarDataAccessor(FusionSolarClient client, IEnumerable<FusionSolarRegisterDefinition> registers)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(registers);
        Client = client;
        Dictionary<ushort, FusionSolarRegisterDefinition> selected = [];
        foreach (var register in registers)
        {
            ArgumentNullException.ThrowIfNull(register);
            register.Validate();
            selected.Add(register.Address, register);
        }
        this.registers = selected;
    }

    private protected FusionSolarDataAccessor(FusionSolarClient client,
        IReadOnlyDictionary<ushort, FusionSolarRegisterDefinition> registers)
    {
        ArgumentNullException.ThrowIfNull(client);
        Client = client;
        this.registers = registers;
    }

    private protected FusionSolarRegisterDefinition GetRegister(ushort address) =>
        registers.TryGetValue(address, out var register) ? register :
            throw new ArgumentException($"Register address {address} is not mapped by this accessor.", nameof(address));

    /// <summary>Explicitly writes an engineering value, enum, text or exact-size structured bytes.</summary>
    /// <remarks>Only mapped RW/WO registers are accepted. Writes are never retried automatically.</remarks>
    public Task WriteValueAsync(ushort address, object value,
        CancellationToken cancellationToken = default)
    {
        var register = GetRegister(address);
        if (!register.CanWrite) throw new InvalidOperationException($"Register '{register.Name}' is read-only.");
        ArgumentNullException.ThrowIfNull(value);
        return Client.WriteRegistersAsync(address, FusionSolarValueCodec.Encode(register, value), cancellationToken);
    }

    /// <summary>Explicitly writes the exact mapped word count without engineering-value validation.</summary>
    /// <remarks>A lost acknowledgement leaves the outcome unknown. Writes are never retried automatically.</remarks>
    public Task WriteRawAsync(ushort address, ReadOnlyMemory<ushort> words,
        CancellationToken cancellationToken = default)
    {
        var register = GetRegister(address);
        if (!register.CanWrite) throw new InvalidOperationException($"Register '{register.Name}' is read-only.");
        if (words.Length != register.WordCount)
            throw new ArgumentException($"Register '{register.Name}' requires {register.WordCount} words.", nameof(words));
        return Client.WriteRegistersAsync(address, words, cancellationToken);
    }
}

/// <summary>Reads a mapped model and explicitly writes selected properties through a client supplied at construction.</summary>
/// <remarks>Metadata, compiled setters and the adjacent-register read plan are cached once per model type.
/// Repeated model reads invoke cached delegates instead of reflection setters.
/// The accessor does not dispose the client, which must outlive it.</remarks>
public sealed class FusionSolarDataAccessor<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : FusionSolarDataAccessor
    where T : new()
{
    private static readonly Lazy<ModelMapping> Mapping = new(CreateMapping);

    public FusionSolarDataAccessor(FusionSolarClient client)
        : base(client ?? throw new ArgumentNullException(nameof(client)), Mapping.Value.Registers) { }

    /// <summary>Reads all mapped properties. Requests never bridge unrequested gaps or exceed 125 words.</summary>
    /// <remarks>A multi-request read is not an atomic snapshot. A write-only mapping rejects a model read before I/O.</remarks>
    public async Task<T> ReadValueAsync(CancellationToken cancellationToken = default)
    {
        var mapping = Mapping.Value;
        if (mapping.WriteOnlyRegister is { } writeOnly)
            throw new InvalidOperationException($"Register '{writeOnly.Name}' is write-only.");
        cancellationToken.ThrowIfCancellationRequested();
        var result = new T();
        foreach (var batch in mapping.Batches)
        {
            var words = await Client.ReadHoldingRegistersAsync(batch.Address, batch.WordCount, cancellationToken).ConfigureAwait(false);
            foreach (var binding in batch.Bindings)
            {
                var span = words.AsSpan(binding.Register.Address - batch.Address, binding.Register.WordCount);
                var value = binding.ValueType == typeof(ushort[]) ? span.ToArray() :
                    FusionSolarValueCodec.Decode(binding.Register, span);
                binding.SetValue(ref result, FusionSolarValueCodec.ConvertValue(value, binding.ValueType));
            }
        }
        return result;
    }

    /// <summary>Writes one directly selected mapped property; it does not write a whole model.</summary>
    public Task WriteAsync<TValue>(Expression<Func<T, TValue>> property, TValue value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(property);
        if (property.Body is not MemberExpression { Member: PropertyInfo selected, Expression: ParameterExpression parameter } ||
            parameter != property.Parameters[0])
            throw new ArgumentException("Select a mapped property directly, for example x => x.PowerFactor.", nameof(property));
        var binding = Mapping.Value.Bindings.FirstOrDefault(binding => binding.Property.HasSameMetadataDefinitionAs(selected))
            ?? throw new ArgumentException($"Property '{selected.Name}' is not mapped by this accessor.", nameof(property));
        return binding.ValueType == typeof(ushort[]) && value is ushort[] words
            ? WriteRawAsync(binding.Register.Address, words, cancellationToken)
            : WriteValueAsync(binding.Register.Address, value!, cancellationToken);
    }

    private delegate void PropertySetter(ref T model, object? value);

    private sealed record PropertyBinding(PropertyInfo Property, Type ValueType,
        FusionSolarRegisterDefinition Register, PropertySetter SetValue);
    private sealed record ReadBatch(ushort Address, ushort WordCount, PropertyBinding[] Bindings);
    private sealed record ModelMapping(IReadOnlyDictionary<ushort, FusionSolarRegisterDefinition> Registers,
        PropertyBinding[] Bindings, ReadBatch[] Batches, FusionSolarRegisterDefinition? WriteOnlyRegister);

    private static PropertySetter CreateSetter(PropertyInfo property)
    {
        // Passing the model by reference updates structs in place without boxing or copying them.
        var model = Expression.Parameter(typeof(T).MakeByRefType(), "model");
        var value = Expression.Parameter(typeof(object), "value");
        var call = Expression.Call(model, property.SetMethod!, Expression.Convert(value, property.PropertyType));
        return Expression.Lambda<PropertySetter>(call, model, value).Compile();
    }

    private static ModelMapping CreateMapping()
    {
        Dictionary<ushort, FusionSolarRegisterDefinition> registers = [];
        List<PropertyBinding> bindings = [];
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            var attribute = property.GetCustomAttribute<FusionSolarDataAttribute>(inherit: true);
            if (attribute is null) continue;
            var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (property.SetMethod is not { IsPublic: true, IsStatic: false } || property.GetIndexParameters().Length != 0 ||
                (!FusionSolarValueCodec.IsNumericType(valueType) && !valueType.IsEnum && valueType != typeof(string) &&
                    valueType != typeof(byte[]) && valueType != typeof(ushort[])))
                throw new ArgumentException($"Attributed property '{typeof(T).Name}.{property.Name}' " +
                    "must be a non-indexed numeric, enum, string, byte[] or ushort[] instance property with a public setter.");
            var write = property.IsDefined(typeof(FusionSolarWriteAttribute));
            var writeOnly = property.IsDefined(typeof(FusionSolarWriteOnlyAttribute));
            if (write && writeOnly) throw new ArgumentException($"Conflicting access markers on '{property.Name}'.");
            var register = new FusionSolarRegisterDefinition(property.Name, attribute.Address, attribute.DataType,
                attribute.WordCount, attribute.Gain == 0 ? null : attribute.Gain, attribute.Unit, !writeOnly, write || writeOnly);
            if (attribute.Name is { } name)
            {
                var source = FusionSolarNameMetadata.GetDefinition(name);
                if (register.DataType != source.DataType || register.WordCount != source.WordCount ||
                    register.Gain != source.Gain || register.Unit != source.Unit)
                    throw new ArgumentException($"Named register metadata cannot be overridden on '{property.Name}'.");
                if ((write && (!source.CanRead || !source.CanWrite)) || (writeOnly && !source.CanWrite))
                    throw new ArgumentException($"Access marker on '{property.Name}' exceeds the named register's capability.");
                register = source with { Name = property.Name, CanRead = source.CanRead && !writeOnly };
            }
            register.Validate();
            if ((valueType == typeof(FusionSolarDeviceStatus) && register.Address != 32089) ||
                (valueType == typeof(FusionSolarMeterStatus) && register.Address != 37100))
                throw new ArgumentException($"Status property '{typeof(T).Name}.{property.Name}' maps to the wrong register.");
            if (registers.TryGetValue(register.Address, out var existing))
            {
                if (register with { Name = existing.Name } != existing)
                    throw new ArgumentException($"Conflicting register metadata at address {register.Address}.");
                register = existing;
            }
            else registers.Add(register.Address, register);
            bindings.Add(new(property, property.PropertyType, register, CreateSetter(property)));
        }
        if (bindings.Count == 0) throw new ArgumentException($"Model '{typeof(T).Name}' has no FusionSolarData properties.");

        var ordered = registers.Values.OrderBy(register => register.Address).ToArray();
        var byAddress = bindings.ToLookup(binding => binding.Register.Address);
        List<ReadBatch> batches = [];
        for (var first = 0; first < ordered.Length;)
        {
            var start = ordered[first].Address;
            var end = (int)start + ordered[first].WordCount;
            var next = first + 1;
            // Never bridge unrequested gaps: devices can reject unsupported addresses inside a read.
            while (next < ordered.Length && ordered[next].Address == end && end + ordered[next].WordCount - start <= 125)
                end += ordered[next++].WordCount;
            var batchBindings = ordered[first..next].SelectMany(register => byAddress[register.Address]).ToArray();
            batches.Add(new(start, (ushort)(end - start), batchBindings));
            first = next;
        }
        return new(registers, bindings.ToArray(), batches.ToArray(), ordered.FirstOrDefault(register => !register.CanRead));
    }
}
