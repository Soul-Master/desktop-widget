using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using System.Xml.Linq;
using Modbus.FusionSolar.Api;
using Modbus.FusionSolar.Api.Models;

using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(45));
Require(FusionSolarNameMetadata.Mapping.Count == 0, "Metadata cache must start empty.");
_ = new SolarDataNameMetadataAttribute(65000, FusionSolarDataType.Int32, 2) { Gain = 1000, Unit = "kW" };
Require(FusionSolarNameMetadata.Mapping.Count == 0, "Constructing metadata must not register it.");
var concurrentMetadata = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ =>
    Task.Run(() => FusionSolarNameMetadata.Get(FusionSolarDataNames.ActivePower))));
Require(concurrentMetadata.All(metadata => ReferenceEquals(metadata, concurrentMetadata[0]) &&
    metadata.Address == 32080 && metadata.DataType == FusionSolarDataType.Int32 &&
    metadata.WordCount == 2 && metadata.Gain == 1000 && metadata.Unit == "kW") &&
    FusionSolarNameMetadata.Mapping.Count == 1, "Concurrent metadata resolution lost named values or loaded unused entries.");
ThrowsSync<NotSupportedException>(() =>
    ((IDictionary<ushort, IFusionSolarNameMetadata>)FusionSolarNameMetadata.Mapping).Clear());
var mutableAttribute = typeof(FusionSolarDataNames).GetField(nameof(FusionSolarDataNames.ActivePower))!
    .GetCustomAttribute<SolarDataNameMetadataAttribute>()!;
mutableAttribute.Gain = 1;
mutableAttribute.Unit = "W";
Require(FusionSolarNameMetadata.Get(FusionSolarDataNames.ActivePower).Gain == 1000 &&
    FusionSolarNameMetadata.Get(FusionSolarDataNames.ActivePower).Unit == "kW",
    "Mutating a reflected attribute changed cached metadata.");
ThrowsSync<ArgumentException>(() => FusionSolarNameMetadata.Get((FusionSolarDataNames)65535));
Require(FusionSolarNameMetadata.Mapping.Count == 1, "An undefined enum value entered the metadata cache.");
Console.WriteLine("PASS on-demand, concurrent and immutable metadata caching; no attribute constructor registration");
await using (var modelSimulator = new Simulator())
await using (var modelClient = new FusionSolarClient("127.0.0.1", new()
{
    Port = modelSimulator.Port, WaitAfterConnect = TimeSpan.Zero, WaitBetweenRequests = TimeSpan.Zero
}))
{
    ThrowsSync<ArgumentNullException>(() => new FusionSolarDataAccessor<SolarData>(null!));
    ThrowsSync<ArgumentNullException>(() => new FusionSolarDataAccessor(null!, []));
    var solarAccessor = new FusionSolarDataAccessor<SolarData>(modelClient);
    Require(FusionSolarNameMetadata.Mapping.Count == typeof(SolarData).GetProperties().Length,
        "Model startup resolved metadata outside the selected model.");
    Require(modelSimulator.Connections == 0, "Creating a client or accessor opened a connection.");
    modelSimulator.Set(32080, 0, 12345);
    SolarData firstReading = await solarAccessor.ReadValueAsync();
    Require(firstReading.ActivePower == 12.345, "The parameterless model read failed.");
    Require((await new FusionSolarDataAccessor<SolarData>(modelClient).ReadValueAsync(stop.Token)).ActivePower == 12.345,
        "Repeated model read failed.");
    Require(modelSimulator.Connections == 1, "Model reads did not reuse the client connection.");
    Require((await new FusionSolarDataAccessor<FusionSolarData>(modelClient).ReadValueAsync(stop.Token)).InverterPower == 12.345,
        "The SolarData alias or struct model lost its decoded value.");
    modelSimulator.Set(32085, 5000);
    var structData = await new FusionSolarDataAccessor<StructSolarData>(modelClient).ReadValueAsync(stop.Token);
    Require(structData.Power == 12.345 && structData.Frequency == 50,
        "Multiple struct properties were not set on the same instance.");
    modelSimulator.Set(32080, 0x7FFF, 0xFFFF);
    structData = await new FusionSolarDataAccessor<StructSolarData>(modelClient).ReadValueAsync(stop.Token);
    Require(structData.Power is null && structData.Frequency == 50,
        "A cached struct setter lost a nullable unavailable value or another property.");
    modelSimulator.Set(32080, 0, 12345);

    var cachedAccessors = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ =>
        Task.Run(() => new FusionSolarDataAccessor<CountedBindingData>(modelClient))));
    modelSimulator.Set(32080, 0, 42000);
    var firstBoundRead = await cachedAccessors[0].ReadValueAsync(stop.Token);
    modelSimulator.Set(32080, 0, 43000);
    var secondBoundRead = await cachedAccessors[1].ReadValueAsync(stop.Token);
    Require(CountedBindingAttribute.ConstructionCount == 1 &&
        firstBoundRead.Value == 42 && secondBoundRead.Value == 43 &&
        firstBoundRead.SetterCalls == 1 && secondBoundRead.SetterCalls == 1,
        "Concurrent accessor creation or repeated reads rebuilt metadata or bypassed a custom setter.");
    await Throws<InvalidOperationException>(() =>
        new FusionSolarDataAccessor<ThrowingSetterData>(modelClient).ReadValueAsync(stop.Token));
    modelSimulator.Set(32080, 0, 12345);

    var settings = new FusionSolarDataAccessor<InverterSettings>(modelClient);
    await settings.WriteAsync(x => x.PowerFactor, -0.95, stop.Token);
    Require(modelSimulator.Get(40122) == 0xFC4A && modelSimulator.Requests.Last().Function == 6,
        "Property-selected write did not encode the setting.");
    Require((await settings.ReadValueAsync(stop.Token)).PowerFactor == -0.95, "Settings readback failed.");
    var inheritedSettings = new FusionSolarDataAccessor<InheritedSettings>(modelClient);
    await inheritedSettings.WriteAsync(x => x.PowerFactor, 0.8, stop.Token);
    Require(modelSimulator.Get(40122) == 800, "Inherited property write failed.");
    Require((await inheritedSettings.ReadValueAsync(stop.Token)).PowerFactor == 0.8,
        "A cached setter failed to bind an inherited property.");
    var rawSettings = new FusionSolarDataAccessor<RawSettings>(modelClient);
    await rawSettings.WriteAsync(x => x.PowerFactorWords, new ushort[] { 800 }, stop.Token);
    await new FusionSolarDataAccessor<WriteOnlyData>(modelClient).WriteAsync(x => x.Value, (ushort)0, stop.Token);
    var beforeInvalid = modelSimulator.Requests.Count;
    await Throws<InvalidOperationException>(() => solarAccessor.WriteAsync(x => x.ActivePower, 1.0, stop.Token));
    await Throws<ArgumentException>(() => settings.WriteAsync(x => x.PowerFactor + 1, 1.0, stop.Token));
    await Throws<ArgumentException>(() => inheritedSettings.WriteAsync(x => x.Unmapped, 1, stop.Token));
    await Throws<ArgumentException>(() => solarAccessor.WriteValueAsync((ushort)FusionSolarDataNames.Model, 1, stop.Token));
    await Throws<InvalidOperationException>(() => new FusionSolarDataAccessor<WriteOnlyData>(modelClient).ReadValueAsync(stop.Token));
    Require(modelSimulator.Requests.Count == beforeInvalid, "Invalid mapping or permissions reached the network.");
    using (var canceledRead = new CancellationTokenSource())
    {
        canceledRead.Cancel();
        await Throws<OperationCanceledException>(() => solarAccessor.ReadValueAsync(canceledRead.Token));
        Require(modelSimulator.Requests.Count == beforeInvalid, "A canceled model read reached the network.");
    }

    modelSimulator.Set(32080, 0, 10000, 0, 20000);
    modelSimulator.Set(32085, 3000);
    var beforeBatch = modelSimulator.Requests.Count;
    var adjacent = await new FusionSolarDataAccessor<AdjacentData>(modelClient).ReadValueAsync(stop.Token);
    var requests = modelSimulator.Requests.Skip(beforeBatch).ToArray();
    Require(adjacent.First == 10 && adjacent.Alias == 10 && adjacent.Second == 20 && adjacent.AfterGap == 30,
        "Batched values or duplicate-address bindings failed.");
    Require(requests.Length == 2 && requests[0].Address == 32080 && requests[0].Words == 4 &&
        requests[1].Address == 32085 && requests[1].Words == 1, "Read bridged an unrequested gap or read a duplicate twice.");
    modelSimulator.Set(32080, 0, 12345);
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<ConflictingData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<WrongStatusData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<ConflictingAccessData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<EmptyData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<NamedReadOnlyWriteData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<NamedGainOverrideData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<NamedUnitOverrideData>(modelClient));
    ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor<UndefinedNameData>(modelClient));
    foreach (var invalid in new[]
    {
        new FusionSolarRegisterDefinition("ZeroWords", 1000, FusionSolarDataType.UInt16, 0, null, null, true, false),
        new FusionSolarRegisterDefinition("Overflow", 65535, FusionSolarDataType.UInt32, 2, null, null, true, false),
        new FusionSolarRegisterDefinition("NegativeGain", 1000, FusionSolarDataType.UInt16, 1, -1, null, true, false)
    })
        ThrowsSync<ArgumentException>(() => new FusionSolarDataAccessor(modelClient, [invalid]));
    await using var otherSimulator = new Simulator();
    await using var otherClient = new FusionSolarClient("127.0.0.1", new()
    {
        Port = otherSimulator.Port, WaitAfterConnect = TimeSpan.Zero, WaitBetweenRequests = TimeSpan.Zero
    });
    otherSimulator.Set(32080, 0, 6789);
    var simultaneous = await Task.WhenAll(solarAccessor.ReadValueAsync(stop.Token), new FusionSolarDataAccessor<SolarData>(otherClient).ReadValueAsync(stop.Token));
    Require(simultaneous[0].ActivePower == 12.345 && simultaneous[1].ActivePower == 6.789 &&
        !ReferenceEquals(simultaneous[0], simultaneous[1]), "Accessors mixed data between their constructor-supplied clients.");
    await using var disposedClient = new FusionSolarClient("127.0.0.1", new() { Port = modelSimulator.Port });
    var disposedAccessor = new FusionSolarDataAccessor<FrequencyData>(disposedClient);
    await disposedClient.DisposeAsync();
    await Throws<ObjectDisposedException>(() => disposedAccessor.ReadValueAsync(stop.Token));
}
Console.WriteLine("PASS one-time binding, concurrent accessor creation, custom/inherited setters, nullable structs, selected writes, connection reuse, batching, gaps and mapping validation");
var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Registers.SPC106.json")));
var expected = fixture.RootElement.GetProperty("registers").EnumerateArray().ToArray();
// Full-definition reflection belongs only in verification, never in the runtime library.
var definitions = ReadDefinitions().ToDictionary(register => register.Address);
Require(expected.Length == 690 && definitions.Count == 690, "There must be exactly 690 register definitions.");
var xmlPath = Path.ChangeExtension(typeof(FusionSolarClient).Assembly.Location, ".xml");
var documentation = XDocument.Load(xmlPath).Descendants("member").ToDictionary(m => (string)m.Attribute("name")!);
foreach (var spec in expected)
{
    var address = spec.GetProperty("address").GetUInt16();
    var actual = definitions[address];
    Require(actual.Name == spec.GetProperty("name").GetString()!.Replace('.', '_') &&
        actual.DataType.ToString() == spec.GetProperty("type").GetString() &&
        actual.WordCount == spec.GetProperty("words").GetUInt16() &&
        actual.Gain == (spec.GetProperty("gain").ValueKind == JsonValueKind.Null ? null : spec.GetProperty("gain").GetInt32()) &&
        actual.Unit == spec.GetProperty("unit").GetString(), $"Source metadata mismatch at {address}.");
    var access = spec.GetProperty("access").GetString();
    Require(actual.CanRead == (access != "WO") && actual.CanWrite == (access != "RO"), $"Incorrect access at {address}.");
    if (documentation.TryGetValue($"F:Modbus.FusionSolar.Api.FusionSolarDataNames.{actual.Name}", out var member))
        Require(!member.Value.Contains("PDF page", StringComparison.OrdinalIgnoreCase), "Source pages must not appear in name documentation.");
}
Require(definitions.Values.Count(r => r.CanRead && !r.CanWrite) == 410 &&
    definitions.Values.Count(r => r.CanRead && r.CanWrite) == 255 &&
    definitions.Values.Count(r => !r.CanRead && r.CanWrite) == 25, "Access totals differ from source.");
Require((ushort)FusionSolarDataNames.EnergyStorageUnit1_BatteryPack1_StateOfCapacity == 38229 &&
    (ushort)FusionSolarDataNames.Dongle_Plant_MaximumChargeFromGridPower == 47590, "Flattened document tag paths differ.");
foreach (ushort removed in new ushort[] {37200,37201,32220,32222,32224,32226,32228,32230})
    Require(!definitions.ContainsKey(removed), $"Removed register {removed} remains defined.");
Require(definitions.Values.All(register => register.Name != "OnlineOptimizers"), "Removed optimizer name remains defined.");
Require(typeof(SolarData).GetProperties().All(p => !p.Name.Contains("Optimizer") && !p.Name.Contains("Storage") &&
    !p.Name.Contains("Voltage") && !p.Name.Contains("Current")), "Removed sample fields remain.");
foreach (var model in new[] { typeof(SolarData), typeof(FusionSolarData) })
foreach (var property in model.GetProperties())
{
    var mapping = property.GetCustomAttribute<FusionSolarDataAttribute>();
    if (mapping is null) continue;
    Require(mapping.Name is not null, $"Bundled mapping must use an enum: {model.Name}.{property.Name}.");
    var source = definitions[mapping.Address];
    Require(mapping.DataType == source.DataType && mapping.WordCount == source.WordCount &&
        (mapping.Gain == 0 ? (int?)null : mapping.Gain) == source.Gain && mapping.Unit == source.Unit,
        $"Named mapping differs from source: {model.Name}.{property.Name}.");
}
Console.WriteLine("PASS all 690 source definitions, flattened names, access flags, documentation rules and removed addresses");

await using var simulator = new Simulator();
await using var client = new FusionSolarClient("127.0.0.1", new()
{
    Port = simulator.Port, Timeout = TimeSpan.FromSeconds(2),
    WaitAfterConnect = TimeSpan.Zero, WaitBetweenRequests = TimeSpan.Zero
});
var accessor = new FusionSolarDataAccessor(client, definitions.Values);
foreach (var register in definitions.Values)
{
    if (register.CanWrite)
    {
        await accessor.WriteRawAsync(register.Address, new ushort[register.WordCount], stop.Token);
        Require(simulator.Requests.Last().Words == register.WordCount, "Wire write size does not match metadata.");
    }
    else
    {
        var count = simulator.Requests.Count;
        await Throws<InvalidOperationException>(() => accessor.WriteRawAsync(register.Address, new ushort[register.WordCount], stop.Token));
        Require(simulator.Requests.Count == count, "RO write reached network.");
    }
}
await CheckCatalogReads(client, simulator, definitions, stop.Token);
Console.WriteLine("PASS every readable catalog mapping, model batching, write permission and exact payload size against loopback simulator");

simulator.Set(32080, 0xFFFF, 0xCFC7); // -12345 / 1000 = -12.345 kW
simulator.Set(32106, 0xB2D0, 0x5E01); // 3,000,000,001 / 100 kWh
simulator.Set(37119, 0xFFFF, 0xCFC7);
simulator.Set(32089, 0x0200);
simulator.Set(37100, 1);
simulator.Set(30000, 0x5355, 0x4E32, 0x3030, 0x3000);
var solar = await new FusionSolarDataAccessor<SolarData>(client).ReadValueAsync(stop.Token);
Require(solar.DeviceStatus == FusionSolarDeviceStatus.OnGrid && solar.MeterStatus == FusionSolarMeterStatus.Normal &&
    solar.ActivePower == -12.345 && solar.AccumulatedYieldEnergy == 30_000_000.01 && solar.MeterPositiveActiveEnergy == -123.45, "Sample bindings mismatch.");
var mixed = await new FusionSolarDataAccessor<MixedData>(client).ReadValueAsync(stop.Token);
Require(mixed.Model == "SUN2000" && mixed.Raw.Length == 15 && mixed.Structure.Length == 8 && mixed.Feature.Length == 4, "Mixed model types mismatch.");
simulator.Set(32085, 0xFFFF);
simulator.Set(32087, 0x7FFF);
simulator.Set(32106, 0xFFFF, 0xFFFF);
simulator.Set(32080, 0x7FFF, 0xFFFF);
var unavailable = await new FusionSolarDataAccessor<SentinelData>(client).ReadValueAsync(stop.Token);
Require(unavailable.Frequency is null && unavailable.Temperature is null && unavailable.Energy is null && unavailable.Power is null,
    "An unavailable marker was lost.");
Require(unavailable.RawPower.SequenceEqual(new ushort[] { 0x7FFF, 0xFFFF }), "Raw sentinel was lost.");
simulator.Set(32089, 0xBEEF);
Require((ushort)(await new FusionSolarDataAccessor<StatusData>(client).ReadValueAsync(stop.Token)).Status!.Value == 0xBEEF, "Unknown status was lost.");
simulator.Set(32089,0xFFFF);
await Throws<InvalidDataException>(() => new FusionSolarDataAccessor<RequiredStatus>(client).ReadValueAsync(stop.Token));
await Throws<InvalidOperationException>(() => new FusionSolarDataAccessor<WriteOnlyData>(client).ReadValueAsync(stop.Token));
Console.WriteLine("PASS signed/scaled/unsigned/text/bytes/raw reads, sample models, null sentinels and unknown status codes");

await accessor.WriteValueAsync((ushort)FusionSolarDataNames.PowerFactorSetting, -0.95, stop.Token);
Require(simulator.Get(40122) == 0xFC4A && simulator.Requests.Last().Function == 6, "Scaled I16/06 write mismatch.");
await accessor.WriteValueAsync((ushort)FusionSolarDataNames.FixedActivePowerDeratingInWatts, 3_000_000_001u, stop.Token);
Require(simulator.Get(40126) == 0xB2D0 && simulator.Get(40127) == 0x5E01 && simulator.Requests.Last().Function == 0x10,
    "U32/10 write mismatch.");
await accessor.WriteValueAsync((ushort)FusionSolarDataNames.SystemTimeLocalTime, 1_700_000_000u, stop.Token);

await accessor.WriteValueAsync((ushort)FusionSolarDataNames.DeviceName,"test",stop.Token);
var written = await new FusionSolarDataAccessor<WrittenData>(client).ReadValueAsync(stop.Token);
Require(written.Time == 1_700_000_000u && written.DeviceName == "test" && simulator.Get(43358) == 0,
    "Epoch U32 or fixed-length text write mismatch.");
byte[] payload = Enumerable.Range(0,82).Select(n => (byte)n).ToArray();
await accessor.WriteValueAsync((ushort)FusionSolarDataNames.EnergyStorage_TimeOfUseElectricityPricePeriods,payload,stop.Token);
Require((await new FusionSolarDataAccessor<WrittenData>(client).ReadValueAsync(stop.Token)).Periods.SequenceEqual(payload),
    "Structured payload mismatch.");
await accessor.WriteValueAsync((ushort)FusionSolarDataNames.Startup,0,stop.Token);
var before = simulator.Requests.Count;
await Throws<InvalidOperationException>(() => accessor.WriteValueAsync((ushort)FusionSolarDataNames.ActivePower,1,stop.Token));
await Throws<ArgumentException>(() => accessor.WriteValueAsync((ushort)FusionSolarDataNames.PowerFactorSetting,0.0001,stop.Token));
await Throws<OverflowException>(() => accessor.WriteValueAsync((ushort)FusionSolarDataNames.PowerFactorSetting,100,stop.Token));
await Throws<ArgumentException>(() => accessor.WriteValueAsync((ushort)FusionSolarDataNames.DeviceName,new string('x',21),stop.Token));
await Throws<ArgumentException>(() => accessor.WriteValueAsync((ushort)FusionSolarDataNames.EnergyStorage_TimeOfUseElectricityPricePeriods,new byte[80],stop.Token));
await Throws<ArgumentException>(() => accessor.WriteRawAsync((ushort)FusionSolarDataNames.DeviceName,new ushort[9],stop.Token));
await Throws<ArgumentException>(() => accessor.WriteRawAsync(37200, new ushort[] { 0 }, stop.Token));
Require(simulator.Requests.Count == before, "Invalid values or removed registers reached network.");
Console.WriteLine("PASS 06/10 writes, engineering-value encoding, text padding, opaque structures and pre-network validation");

var exceptionCases = new (byte Code, FusionSolarModbusExceptionCode? KnownCode, string Label, string Detail)[]
{
    (0x01, FusionSolarModbusExceptionCode.IllegalFunction, "Illegal function", "current state"),
    (0x02, FusionSolarModbusExceptionCode.IllegalDataAddress, "Illegal data address", "transfer length"),
    (0x03, FusionSolarModbusExceptionCode.IllegalDataValue, "Illegal data value", "request data or structure"),
    (0x04, FusionSolarModbusExceptionCode.SlaveNodeFailure, "Slave node failure", "performing the requested action"),
    (0x06, FusionSolarModbusExceptionCode.SlaveDeviceBusy, "Slave device busy", "caller decides"),
    (0x80, FusionSolarModbusExceptionCode.NoPermission, "No permission", "expired"),
    (0x00, null, "Unknown exception code", "not defined"),
    (0x05, null, "Unknown exception code", "not defined"),
    (0xFF, null, "Unknown exception code", "not defined")
};
Require(Enum.GetValues<FusionSolarModbusExceptionCode>().Select(code => (byte)code)
    .SequenceEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x06, 0x80 }), "Exception codes differ from section 6.3.2.");
foreach (var (code, knownCode, label, detail) in exceptionCases)
foreach (byte function in new byte[] { 3, 6, 0x10 })
{
    var address = function switch
    {
        3 => (ushort)FusionSolarDataNames.GridFrequency,
        6 => (ushort)FusionSolarDataNames.Startup,
        _ => (ushort)FusionSolarDataNames.FixedActivePowerDeratingInWatts
    };
    simulator.Set(address, 123, 456);
    simulator.Faults.Enqueue($"exception:{code:X2}");
    before = simulator.Requests.Count;
    var connections = simulator.Connections;
    try
    {
        if (function == 3) await new FusionSolarDataAccessor<FrequencyData>(client).ReadValueAsync(stop.Token);
        else await accessor.WriteRawAsync(address, function == 6 ? new ushort[] { 0 } : new ushort[] { 0, 0 }, stop.Token);
        throw new Exception("Expected device exception.");
    }
    catch (FusionSolarModbusException ex)
    {
        Require(ex.FunctionCode == function && ex.ExceptionCode == code && ex.KnownExceptionCode == knownCode,
            "Wrong raw or typed exception details.");
        Require(ex.Description.Contains(label) && ex.Description.Contains(detail) && ex.Message.Contains(ex.Description) &&
            ex.Message.Contains($"0x{function:X2}") && ex.Message.Contains($"0x{code:X2}"), "Missing readable exception details.");
    }
    Require(simulator.Requests.Count == before + 1, "A device exception triggered an automatic retry.");
    Require(simulator.Get(address) == 123 && simulator.Get((ushort)(address + 1)) == 456,
        "The simulator applied a rejected write.");
    await new FusionSolarDataAccessor<FrequencyData>(client).ReadValueAsync(stop.Token);
    Require(simulator.Connections == connections, "A complete exception frame caused a reconnect.");
}
Console.WriteLine("PASS all six section 6.3.2 codes and unknown codes for 03/06/10, readable errors, connection reuse and no automatic retry");

simulator.Faults.Enqueue("badAck");
before = simulator.Requests.Count;
await Throws<InvalidDataException>(() => accessor.WriteValueAsync((ushort)FusionSolarDataNames.Startup,0,stop.Token));
Require(simulator.Requests.Count == before+1,"A write was retried automatically.");
await accessor.WriteValueAsync((ushort)FusionSolarDataNames.Startup,0,stop.Token);
simulator.Faults.Enqueue("truncated");
await Throws<EndOfStreamException>(() => new FusionSolarDataAccessor<FrequencyData>(client).ReadValueAsync(stop.Token));
await new FusionSolarDataAccessor<FrequencyData>(client).ReadValueAsync(stop.Token);
await Task.WhenAll(Enumerable.Range(0,10).Select(_ => accessor.WriteValueAsync((ushort)FusionSolarDataNames.Startup,0,stop.Token)));
Console.WriteLine("PASS malformed acknowledgement, partial reply, reconnection and serialized concurrent writes");

await using (var shortClient = new FusionSolarClient("127.0.0.1",new()
{
    Port=simulator.Port,Timeout=TimeSpan.FromMilliseconds(100),WaitAfterConnect=TimeSpan.Zero,WaitBetweenRequests=TimeSpan.Zero
}))
{
    var shortAccessor = new FusionSolarDataAccessor(shortClient, definitions.Values);
    simulator.Faults.Enqueue("delay");
    before=simulator.Requests.Count;
    await Throws<TimeoutException>(() => shortAccessor.WriteValueAsync((ushort)FusionSolarDataNames.Startup,0,stop.Token));
    Require(simulator.Requests.Count==before+1,"Timed-out write retried.");
    await new FusionSolarDataAccessor<FrequencyData>(shortClient).ReadValueAsync(stop.Token);
    using var canceled = new CancellationTokenSource();
    canceled.Cancel();
    await Throws<OperationCanceledException>(() => shortAccessor.WriteValueAsync((ushort)FusionSolarDataNames.Startup,0,canceled.Token));
}
Console.WriteLine("PASS timeout/cancellation behavior and no automatic write retry; no real device contacted");

static async Task CheckCatalogReads(FusionSolarClient client, Simulator simulator,
    IReadOnlyDictionary<ushort, FusionSolarRegisterDefinition> definitions, CancellationToken cancellationToken)
{
    // Build a test-only model so all readable catalog entries exercise the public whole-model API.
    var module = AssemblyBuilder.DefineDynamicAssembly(new("CatalogReadChecks"), AssemblyBuilderAccess.Run)
        .DefineDynamicModule("CatalogReadChecks");
    var builder = module.DefineType("CatalogReadData", TypeAttributes.Public);
    builder.DefineDefaultConstructor(MethodAttributes.Public);
    var readable = definitions.Values.Where(register => register.CanRead).ToArray();
    var attributeConstructor = typeof(FusionSolarDataAttribute).GetConstructor([typeof(FusionSolarDataNames)])!;
    foreach (var register in readable)
    {
        var field = builder.DefineField("_" + register.Name, typeof(ushort[]), FieldAttributes.Private);
        var property = builder.DefineProperty(register.Name, PropertyAttributes.None, typeof(ushort[]), null);
        var getter = builder.DefineMethod("get_" + register.Name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(ushort[]), Type.EmptyTypes);
        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);
        var setter = builder.DefineMethod("set_" + register.Name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(void), [typeof(ushort[])]);
        il = setter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        property.SetSetMethod(setter);
        property.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [(FusionSolarDataNames)register.Address]));
    }
    var modelType = builder.CreateType()!;
    var accessorType = typeof(FusionSolarDataAccessor<>).MakeGenericType(modelType);
    var accessor = Activator.CreateInstance(accessorType, client)!;
    var readMethod = accessorType.GetMethod("ReadValueAsync")!;
    var before = simulator.Requests.Count;
    var read = (Task)readMethod.Invoke(accessor, [cancellationToken])!;
    await read;
    var model = readMethod.ReturnType.GetProperty("Result")!.GetValue(read)!;
    foreach (var register in readable)
    {
        var words = (ushort[])modelType.GetProperty(register.Name)!.GetValue(model)!;
        Require(words.Length == register.WordCount &&
            words.SequenceEqual(Enumerable.Range(register.Address, register.WordCount).Select(address => simulator.Get((ushort)address))),
            $"Incorrect catalog model words at {register.Address}.");
    }
    var requestedWords = readable.SelectMany(register => Enumerable.Range(register.Address, register.WordCount)).ToHashSet();
    var requests = simulator.Requests.Skip(before).ToArray();
    Require(requests.All(request => request.Function == 3 && request.Words <= 125 &&
        Enumerable.Range(request.Address, request.Words).All(requestedWords.Contains)),
        "A catalog read exceeded 125 words or bridged an unrequested gap.");
    Require(requests.SelectMany(request => Enumerable.Range(request.Address, request.Words)).ToHashSet().SetEquals(requestedWords),
        "A readable catalog address was not requested.");
}

static IEnumerable<FusionSolarRegisterDefinition> ReadDefinitions()
{
    Require(Enum.GetUnderlyingType(typeof(FusionSolarDataNames)) == typeof(ushort), "Register enum must use ushort.");
    foreach (var field in typeof(FusionSolarDataNames).GetFields(BindingFlags.Public | BindingFlags.Static))
    {
        Require(field.IsLiteral && field.FieldType == typeof(FusionSolarDataNames),
            $"Register must be an enum member: {field.Name}.");
        var metadata = field.GetCustomAttribute<SolarDataNameMetadataAttribute>()
            ?? throw new InvalidOperationException($"Missing metadata: {field.Name}.");
        Require(metadata.Address == (ushort)field.GetRawConstantValue()!,
            $"Missing metadata or incorrect enum address: {field.Name}.");
        var write = field.IsDefined(typeof(FusionSolarWriteAttribute));
        var writeOnly = field.IsDefined(typeof(FusionSolarWriteOnlyAttribute));
        Require(!(write && writeOnly) && metadata.WordCount is >= 1 and <= 125 && metadata.Gain >= 0,
            $"Invalid register metadata: {field.Name}.");
        var definition = new FusionSolarRegisterDefinition(field.Name, metadata.Address, metadata.DataType,
            metadata.WordCount, metadata.Gain == 0 ? null : metadata.Gain, metadata.Unit, !writeOnly, write || writeOnly);
        var resolved = FusionSolarNameMetadata.Get((FusionSolarDataNames)metadata.Address);
        Require(resolved.Address == metadata.Address && resolved.DataType == metadata.DataType &&
            resolved.WordCount == metadata.WordCount && resolved.Gain == metadata.Gain && resolved.Unit == metadata.Unit,
            $"Resolved metadata differs from enum attribute: {field.Name}.");
        yield return definition;
    }
}

static void Require(bool condition,string message) { if (!condition) throw new Exception(message); }
static void ThrowsSync<T>(Action action) where T:Exception
{
    try { action(); } catch(T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
static async Task Throws<T>(Func<Task> action) where T:Exception
{
    try { await action(); } catch(T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}

public class MixedData
{
    [FusionSolarData(FusionSolarDataNames.Model)] public string? Model {get;set;}
    [FusionSolarData(FusionSolarDataNames.Model)] public ushort[] Raw {get;set;}=[];
    [FusionSolarData(FusionSolarDataNames.ActivePowerAdjustmentStatus)] public byte[] Structure {get;set;}=[];
    [FusionSolarData(FusionSolarDataNames.FeatureCode1)] public byte[] Feature {get;set;}=[];
}
public class StatusData { [FusionSolarData(FusionSolarDataNames.DeviceStatus)] public FusionSolarDeviceStatus? Status {get;set;} }
public class RequiredStatus { [FusionSolarData(FusionSolarDataNames.DeviceStatus)] public FusionSolarDeviceStatus Status {get;set;} }
public class WriteOnlyData { [FusionSolarData(FusionSolarDataNames.Startup)] public ushort Value {get;set;} }

public struct StructSolarData
{
    [SolarData(FusionSolarDataNames.ActivePower)] public double? Power { get; set; }
    [SolarData(FusionSolarDataNames.GridFrequency)] public double? Frequency { get; set; }
}
public sealed class CountedBindingAttribute : FusionSolarDataAttribute
{
    public static int ConstructionCount;
    public CountedBindingAttribute() : base(FusionSolarDataNames.ActivePower) =>
        Interlocked.Increment(ref ConstructionCount);
}
public class CountedBindingData
{
    private double? value;
    public int SetterCalls { get; private set; }
    [CountedBinding]
    public double? Value
    {
        get => value;
        set { this.value = value; SetterCalls++; }
    }
}
public class ThrowingSetterData
{
    [FusionSolarData(FusionSolarDataNames.ActivePower)]
    public double? Value { get => 0; set => throw new InvalidOperationException("Setter rejected the value."); }
}
public class NamedReadOnlyWriteData
{
    [FusionSolarData(FusionSolarDataNames.ActivePower), FusionSolarWrite] public double? Power { get; set; }
}
public class NamedGainOverrideData
{
    [FusionSolarData(FusionSolarDataNames.ActivePower, Gain = 1)] public double? Power { get; set; }
}
public class NamedUnitOverrideData
{
    [FusionSolarData(FusionSolarDataNames.ActivePower, Unit = "W")] public double? Power { get; set; }
}
public class UndefinedNameData
{
    [FusionSolarData((FusionSolarDataNames)65535)] public double? Power { get; set; }
}

public class AdjacentData
{
    [FusionSolarData(FusionSolarDataNames.ActivePower)] public double? First { get; set; }
    [FusionSolarData(FusionSolarDataNames.ActivePower)] public double? Alias { get; set; }
    [FusionSolarData(FusionSolarDataNames.ReactivePower)] public double? Second { get; set; }
    [FusionSolarData(FusionSolarDataNames.GridFrequency)] public double? AfterGap { get; set; }
}
public class ConflictingData
{
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting)] public double? A { get; set; }
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting), FusionSolarWriteOnly] public double? B { get; set; }
}
public class WrongStatusData
{
    [FusionSolarData(FusionSolarDataNames.GridFrequency)] public FusionSolarDeviceStatus Status { get; set; }
}
public class ConflictingAccessData
{
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting), FusionSolarWrite, FusionSolarWriteOnly]
    public double Value { get; set; }
}
public class InverterSettings
{
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting), FusionSolarWrite]
    public double? PowerFactor { get; set; }
}
public class FrequencyData
{
    [FusionSolarData(FusionSolarDataNames.GridFrequency)] public double? Frequency { get; set; }
}
public class SentinelData
{
    [FusionSolarData(FusionSolarDataNames.GridFrequency)] public double? Frequency { get; set; }
    [FusionSolarData(FusionSolarDataNames.InternalTemperature)] public double? Temperature { get; set; }
    [FusionSolarData(FusionSolarDataNames.TotalYield)] public double? Energy { get; set; }
    [FusionSolarData(FusionSolarDataNames.ActivePower)] public double? Power { get; set; }
    [FusionSolarData(FusionSolarDataNames.ActivePower)] public ushort[] RawPower { get; set; } = [];
}
public class WrittenData
{
    [FusionSolarData(FusionSolarDataNames.SystemTimeLocalTime)] public uint Time { get; set; }
    [FusionSolarData(FusionSolarDataNames.DeviceName)] public string? DeviceName { get; set; }
    [FusionSolarData(FusionSolarDataNames.EnergyStorage_TimeOfUseElectricityPricePeriods)] public byte[] Periods { get; set; } = [];
}
public class EmptyData;
public class InheritedSettings : InverterSettings { public int Unmapped { get; set; } }
public class RawSettings
{
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting)]
    [FusionSolarWrite]
    public ushort[] PowerFactorWords { get; set; } = [];
}

sealed class Simulator : IAsyncDisposable
{
    private readonly TcpListener listener = new(IPAddress.Loopback,0);
    private readonly CancellationTokenSource stop = new();
    private readonly ConcurrentDictionary<ushort,ushort> memory=[];
    private readonly List<Task> sessions=[];
    private readonly Task server;
    private int connections;
    public ConcurrentQueue<(byte Function,ushort Address,ushort Words)> Requests {get;}=[];
    public ConcurrentQueue<string> Faults {get;}=[];
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public int Connections => Volatile.Read(ref connections);
    public Simulator() { listener.Start(); server=Serve(); }
    public void Set(ushort address,params ushort[] words) { for(var i=0;i<words.Length;i++) memory[(ushort)(address+i)]=words[i]; }
    public ushort Get(ushort address)=>memory.GetValueOrDefault(address);
    private async Task Serve()
    {
        try
        {
            while(!stop.IsCancellationRequested)
            {
                var socket = await listener.AcceptTcpClientAsync(stop.Token);
                Interlocked.Increment(ref connections);
                sessions.Add(Handle(socket));
            }
        }
        catch(OperationCanceledException) when(stop.IsCancellationRequested) { }
    }
    private async Task Handle(TcpClient socket)
    {
        using(socket)
        try
        {
            var stream=socket.GetStream();
            while(!stop.IsCancellationRequested)
            {
                byte[] header=new byte[7];
                await stream.ReadExactlyAsync(header,stop.Token);
                RequireUnit(header[6]);
                byte[] request=new byte[BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4))-1];
                await stream.ReadExactlyAsync(request,stop.Token);
                var function=request[0];
                var address=BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(1));
                var count=function==6 ? (ushort)1 : BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(3));
                Requests.Enqueue((function,address,count));
                Faults.TryDequeue(out var fault);
                byte[] response;
                if(fault?.StartsWith("exception:", StringComparison.Ordinal) == true)
                    response=[(byte)(function|0x80),Convert.ToByte(fault["exception:".Length..],16)];
                else if(function==3)
                {
                    response=new byte[2+count*2]; response[0]=3;response[1]=(byte)(count*2);
                    for(var i=0;i<count;i++) BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(2+i*2),Get((ushort)(address+i)));
                }
                else
                {
                    if(function!=6 && function!=0x10) throw new Exception("Unexpected write function.");
                    if(function==0x10 && (request.Length!=6+count*2 || request[5]!=count*2)) throw new Exception("Invalid multi-write payload.");
                    for(var i=0;i<count;i++) memory[(ushort)(address+i)]=BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(function==6 ? 3 : 6+i*2));
                    response=request[..5];
                }
                if(fault=="badAck") response[^1]^=1;
                if(fault=="delay") await Task.Delay(400,stop.Token);
                BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4),(ushort)(response.Length+1));
                await stream.WriteAsync(header,stop.Token);
                if(fault=="truncated") { await stream.WriteAsync(response[..1],stop.Token); return; }
                // Fragment replies to ensure frame reads do not depend on one socket read.
                await stream.WriteAsync(response[..1],stop.Token);
                await stream.WriteAsync(response[1..],stop.Token);
            }
        }
        catch(OperationCanceledException) when(stop.IsCancellationRequested) { }
        catch(IOException) { }
        catch(SocketException) when(stop.IsCancellationRequested) { }
    }
    private static void RequireUnit(byte value) { if(value!=1) throw new Exception("Default unit ID must be 1."); }
    public async ValueTask DisposeAsync()
    {
        stop.Cancel();listener.Stop();await server;await Task.WhenAll(sessions);stop.Dispose();
    }
}
