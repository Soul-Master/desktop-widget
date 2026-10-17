using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
    var solarAccessor = new FusionSolarDataAccessor<SolarData>();
    Require(FusionSolarNameMetadata.Mapping.Count == typeof(SolarData).GetProperties().Length,
        "Model startup resolved metadata outside the selected model.");
    Require(modelSimulator.Connections == 0, "Creating a client or accessor opened a connection.");
    modelSimulator.Set(32080, 0, 12345);
    Require((await solarAccessor.ReadAsync(modelClient, stop.Token)).ActivePower == 12.345, "Self-contained mapping failed.");
    Require((await new FusionSolarDataAccessor<SolarData>().ReadAsync(modelClient, stop.Token)).ActivePower == 12.345,
        "Repeated model read failed.");
    Require(modelSimulator.Connections == 1, "Model reads did not reuse the client connection.");
    Require((await new FusionSolarDataAccessor<FusionSolarData>().ReadAsync(modelClient, stop.Token)).InverterPower == 12.345,
        "The SolarData alias or struct model lost its decoded value.");
    modelSimulator.Set(32085, 5000);
    var structData = await new FusionSolarDataAccessor<StructSolarData>().ReadAsync(modelClient, stop.Token);
    Require(structData.Power == 12.345 && structData.Frequency == 50,
        "Multiple struct properties were not set on the same instance.");
    modelSimulator.Set(32080, 0x7FFF, 0xFFFF);
    structData = await new FusionSolarDataAccessor<StructSolarData>().ReadAsync(modelClient, stop.Token);
    Require(structData.Power is null && structData.Frequency == 50,
        "A cached struct setter lost a nullable unavailable value or another property.");
    modelSimulator.Set(32080, 0, 12345);

    var cachedAccessors = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ =>
        Task.Run(() => new FusionSolarDataAccessor<CountedBindingData>())));
    modelSimulator.Set(32080, 0, 42000);
    var firstBoundRead = await cachedAccessors[0].ReadAsync(modelClient, stop.Token);
    modelSimulator.Set(32080, 0, 43000);
    var secondBoundRead = await cachedAccessors[1].ReadAsync(modelClient, stop.Token);
    Require(CountedBindingAttribute.ConstructionCount == 1 &&
        firstBoundRead.Value == 42 && secondBoundRead.Value == 43 &&
        firstBoundRead.SetterCalls == 1 && secondBoundRead.SetterCalls == 1,
        "Concurrent accessor creation or repeated reads rebuilt metadata or bypassed a custom setter.");
    await Throws<InvalidOperationException>(() =>
        new FusionSolarDataAccessor<ThrowingSetterData>().ReadAsync(modelClient, stop.Token));
    modelSimulator.Set(32080, 0, 12345);

    var settings = new FusionSolarDataAccessor<InverterSettings>();
    await settings.WriteAsync(modelClient, x => x.PowerFactor, -0.95, stop.Token);
    Require(modelSimulator.Get(40122) == 0xFC4A && modelSimulator.Requests.Last().Function == 6,
        "Property-selected write did not encode the setting.");
    Require((await settings.ReadAsync(modelClient, stop.Token)).PowerFactor == -0.95, "Settings readback failed.");
    var inheritedSettings = new FusionSolarDataAccessor<InheritedSettings>();
    await inheritedSettings.WriteAsync(modelClient, x => x.PowerFactor, 0.8, stop.Token);
    Require(modelSimulator.Get(40122) == 800, "Inherited property write failed.");
    Require((await inheritedSettings.ReadAsync(modelClient, stop.Token)).PowerFactor == 0.8,
        "A cached setter failed to bind an inherited property.");
    var rawSettings = new FusionSolarDataAccessor<RawSettings>();
    await rawSettings.WriteAsync(modelClient, x => x.PowerFactorWords, new ushort[] { 800 }, stop.Token);
    await new FusionSolarDataAccessor<WriteOnlyData>().WriteAsync(modelClient, x => x.Value, (ushort)0, stop.Token);
    var beforeInvalid = modelSimulator.Requests.Count;
    await Throws<InvalidOperationException>(() => solarAccessor.WriteAsync(modelClient, x => x.ActivePower, 1.0, stop.Token));
    await Throws<ArgumentException>(() => settings.WriteAsync(modelClient, x => x.PowerFactor + 1, 1.0, stop.Token));
    await Throws<ArgumentException>(() => inheritedSettings.WriteAsync(modelClient, x => x.Unmapped, 1, stop.Token));
    await Throws<ArgumentException>(() => solarAccessor.ReadRawAsync(modelClient, (ushort)FusionSolarDataNames.Model, stop.Token));
    await Throws<InvalidOperationException>(() => new FusionSolarDataAccessor<WriteOnlyData>().ReadAsync(modelClient, stop.Token));
    Require(modelSimulator.Requests.Count == beforeInvalid, "Invalid mapping or permissions reached the network.");

    modelSimulator.Set((ushort)FusionSolarDataNames.Model, 0x4142);
    modelSimulator.Set(32089, 0xBEEF);
    var mixed = await new FusionSolarDataAccessor<MixedData>().ReadAsync(modelClient, stop.Token);
    Require(mixed.Model == "AB" && mixed.Raw[0] == 0x4142 && mixed.Structure.Length > 0,
        "Cached setters lost text, bytes or duplicate-address raw words.");
    var mixedStruct = await new FusionSolarDataAccessor<MixedStructData>().ReadAsync(modelClient, stop.Token);
    Require(mixedStruct.Model == "AB" && mixedStruct.Raw[0] == 0x4142 &&
        mixedStruct.Structure.Length > 0 && (ushort)mixedStruct.Status!.Value == 0xBEEF,
        "Struct reference or nullable enum setters failed.");
    modelSimulator.Set(32089, 0xFFFF);
    Require((await new FusionSolarDataAccessor<MixedStructData>().ReadAsync(modelClient, stop.Token)).Status is null,
        "A null enum failed to bind to a struct property.");
    await Throws<InvalidDataException>(() => new FusionSolarDataAccessor<RequiredStatus>().ReadAsync(modelClient, stop.Token));
    var virtualData = await new FusionSolarDataAccessor<VirtualBindingData>().ReadAsync(modelClient, stop.Token);
    Require(virtualData.Power == 12.345 && virtualData.SetterCalls == 1,
        "Inherited attribute or virtual override setter dispatch failed.");
    await using var otherSimulator = new Simulator();
    await using var otherClient = new FusionSolarClient("127.0.0.1", new()
    {
        Port = otherSimulator.Port, WaitAfterConnect = TimeSpan.Zero, WaitBetweenRequests = TimeSpan.Zero
    });
    otherSimulator.Set(32080, 0, 6789);
    var simultaneous = await Task.WhenAll(solarAccessor.ReadAsync(modelClient, stop.Token), solarAccessor.ReadAsync(otherClient, stop.Token));
    Require(simultaneous[0].ActivePower == 12.345 && simultaneous[1].ActivePower == 6.789 &&
        !ReferenceEquals(simultaneous[0], simultaneous[1]), "Accessor mixed data between supplied clients.");
}
Console.WriteLine("PASS cached binding: concurrent construction, repeated reads, structs, nullable enums, custom/inherited/virtual setters, text/bytes/raw words, writes and concurrent clients");
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

public class InverterSettings
{
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting), FusionSolarWrite]
    public double? PowerFactor { get; set; }
}
public struct MixedStructData
{
    [FusionSolarData(FusionSolarDataNames.Model)] public string? Model {get;set;}
    [FusionSolarData(FusionSolarDataNames.Model)] public ushort[] Raw {get;set;}
    [FusionSolarData(FusionSolarDataNames.ActivePowerAdjustmentStatus)] public byte[] Structure {get;set;}
    [FusionSolarData(FusionSolarDataNames.DeviceStatus)] public FusionSolarDeviceStatus? Status {get;set;}
}
public class VirtualBindingBase
{
    [SolarData(FusionSolarDataNames.ActivePower)] public virtual double? Power {get;set;}
}
public class VirtualBindingData : VirtualBindingBase
{
    public int SetterCalls {get;private set;}
    public override double? Power {get => base.Power; set {base.Power = value; SetterCalls++;}}
}
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
