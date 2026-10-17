# Modbus.FusionSolar.Api

C# API for local Huawei SUN2000MA Modbus TCP, targeting .NET 10 without external
NuGet dependencies. Register definitions follow the reviewed SPC106 document;
this is not the FusionSolar cloud API. See [source notes](REGISTER-SOURCES.md).

## Read a model through a client supplied at construction

```csharp
using Modbus.FusionSolar.Api;
using Modbus.FusionSolar.Api.Models;

await using var client = new FusionSolarClient("192.168.68.55", new()
{
    Port = 502,
    UnitId = 1
});
var solar = new FusionSolarDataAccessor<SolarData>(client);
SolarData data = await solar.ReadValueAsync(cancellationToken);
Console.WriteLine($"AC output: {data.ActivePower} kW");
Console.WriteLine($"Today's yield: {data.DailyYieldEnergy} kWh");
```

The client owns connection lifetime, Modbus framing, timeouts and serialized
requests. It opens TCP lazily on the first accessor operation. Every accessor
requires a client in its constructor and uses that client for all reads and
writes. Reuse the accessor when polling; create a separate accessor for another
client. Accessors do not dispose the client, which must outlive them. The client
has no public data read/write methods; these belong to `FusionSolarDataAccessor`.

Defaults are port 502, unit ID 1, a ten-second request timeout, one second after
connection and 50 milliseconds between requests. Local Modbus must be enabled
and the selected registers must match the device hardware and firmware. Use
cancellation to bound an entire model read.

## Define only the data you need

```csharp
public struct MySolarData
{
    [SolarData(FusionSolarDataNames.Model)]
    public string? Model { get; set; }

    [SolarData(FusionSolarDataNames.ActivePower)]
    public double? ActivePowerKw { get; set; }
}
```

`[SolarData(name)]` and `[FusionSolarData(name)]` accept compile-time enum members.
Classes with public parameterless constructors and structs are supported. Each
enum member carries `SolarDataNameMetadata` for its address, wire type, word count,
gain and unit, plus the existing access markers. Enum-based properties inherit
the documented access mode; they cannot grant writes to read-only registers or
override gain/unit. `[FusionSolarWriteOnly]` can narrow a RW property to WO.
Access modes describe documented capability, not device authorization.

Metadata is reflected only for requested names, validated, and cached by address.
`FusionSolarNameMetadata.Mapping` exposes a read-only view of immutable snapshots;
`FusionSolarNameMetadata.Get(name)` resolves one entry on demand. Metadata attribute
constructors do not register themselves: named `Gain` and `Unit` arguments are
applied only after construction. Zero gain preserves an unspecified source gain;
unscaled values remain raw numbers. No full-catalog metadata scan is performed.

Only attributed public instance properties with public setters are mapped.
Numeric types, enums, `string`, `byte[]` and `ushort[]` are supported. Numeric
values are divided by the gain. Integer properties reject fractions and overflow;
nullable properties accept unavailable numeric markers. `ushort[]` preserves raw
words, padding and markers. Text is strict UTF-8 with trailing NUL padding removed.
Structured or inconsistent scalar definitions remain opaque bytes.

The accessor reflects and validates the selected mappings once on first use,
compiles setter delegates, and caches them with the adjacent-register request
plan per model type. Repeated model reads invoke those delegates without property
reflection or boxing the model; structs are updated by reference. Values still
pass through the existing object-based codec, so numeric boxing and runtime type
conversion remain. Raw arrays are copied when required. Custom setter exceptions
propagate directly instead of being wrapped in `TargetInvocationException`.
Duplicate addresses are read once; contradictory definitions are rejected.
Adjacent requested registers are batched up to 125 words without bridging gaps.
Every attributed property is requested. A write-only property makes a whole-model
read fail before I/O; it remains usable for explicit writes. Use a separate model
containing readable properties for model reads.

This removes the full-catalog scan from model startup and avoids rebuilding the
read plan on every poll. It does not establish the fastest startup or reads:
reflection and delegate compilation still run once per model type, and connection
delays, device latency and the number of requests affect total time. These are
runtime bindings, not compile-time source generation. Property-selected writes
still inspect the supplied expression's property metadata. Native AOT/trimming
and expression-interpreter performance are not validated.

## Read the mapped model

```csharp
var solar = new FusionSolarDataAccessor<MySolarData>(client);
MySolarData result = await solar.ReadValueAsync();
Console.WriteLine(result.ActivePowerKw);

// Supply a cancellation token when needed.
MySolarData next = await solar.ReadValueAsync(cancellationToken);
```

`ReadValueAsync()` returns a new `T` containing every mapped property. There are
no public single-register read methods. To read fewer values, define a smaller
model. To retain raw words, map a `ushort[]` property:

```csharp
public class RawModelData
{
    [FusionSolarData(FusionSolarDataNames.Model)]
    public ushort[] ModelWords { get; set; } = [];
}
```

## Explicit writes

Define a settings model for a mapped RW power-factor setting. Select one
property to write; models are never written back in bulk:

```csharp
var settings = new FusionSolarDataAccessor<InverterSettings>(client);
await settings.WriteAsync(x => x.PowerFactor, -0.95, cancellationToken);

public class InverterSettings
{
    [FusionSolarData(FusionSolarDataNames.PowerFactorSetting)]
    public double? PowerFactor { get; set; }
}
```

Address-based `WriteValueAsync(address, value, token)` and
`WriteRawAsync(address, words, token)` are also available for mapped RW/WO
registers. Engineering-value writes validate scaling, exact representability,
signedness and payload size. A selected `ushort[]` property uses raw words.
Raw writes enforce the word count and permission but bypass semantic validation.
Single-word writes use function 06; multiple-word writes use function 10.

Device scopes, authentication, model options and prerequisites still apply. A
valid wire value can be rejected by the device. There is no automatic write
retry: a timeout or lost acknowledgement leaves the outcome unknown.

Enum mappings use validated catalog metadata and access capabilities. For
explicit write definitions without a model, construct the non-generic accessor
with `new FusionSolarDataAccessor(client, registerDefinitions)`. Caller-supplied
definitions are not cross-checked against the catalog; a wrong address, type,
gain or access flag can write the wrong data. The reviewed catalog still does
not establish device support.

## Register definitions and migration

`FusionSolarDataNames : ushort` contains all 690 reviewed addresses, with wire
metadata and XML documentation on its members. Original nested document groups
are flattened with underscores, for example
`EnergyStorageUnit1_BatteryPack1_StateOfCapacity`. Access markers remain on the
enum fields. To inspect one entry:

```csharp
var register = FusionSolarNameMetadata.Get(FusionSolarDataNames.ActivePower);
Console.WriteLine($"{register.Address}: {register.DataType}, {register.WordCount} words, gain {register.Gain}, {register.Unit}");
// 32080: Int32, 2 words, gain 1000, kW
```

Cast an enum member to `ushort` when an API needs a raw address. The enum makes
attribute references compile-time constants; metadata reflection and validation
still happen on first use at runtime. An explicit cast of an undefined numeric
value to the enum is rejected during resolution. Native AOT/trimming is not
validated. Metadata resolution does not contact the inverter.

Migration from earlier accessor versions:

- Construct accessors with `new FusionSolarDataAccessor<SolarData>(client)` and
  reuse them when polling.
- Replace model `ReadAsync(client, token)` calls with `ReadValueAsync(token)`.
- Replace address-based reads with properties on a model, then read that model
  with `ReadValueAsync()`.
- Remove the client argument from accessor write calls; writes use the client
  supplied at construction.
- Replace the former struct catalog with `FusionSolarDataNames`; change nested
  name separators to underscores. Use `(ushort)FusionSolarDataNames.Name` for raw addresses.
- Use `[SolarData(FusionSolarDataNames.Name)]` or `[FusionSolarData(FusionSolarDataNames.Name)]`
  on model properties.
- `IFusionSolarNameMetadata` and the shared mapping are read-only. Configure gain/unit
  on the enum's metadata attribute, rather than mutating the cache.
- Use a settings model for explicit property writes. `SolarData` is read-only.

## Handle device exceptions

Functions 03/06/10 preserve complete device exception frames and keep the
connection usable. `FusionSolarModbusException` identifies the codes in SPC106
section 6.3.2 and includes readable `Message` and `Description` properties:

| Raw code | `FusionSolarModbusExceptionCode` | Meaning |
| --- | --- | --- |
| `0x01` | `IllegalFunction` | Unsupported function or unsuitable device state. |
| `0x02` | `IllegalDataAddress` | Invalid register address/transfer-length combination. |
| `0x03` | `IllegalDataValue` | Invalid request data or structure, such as an incorrect length. |
| `0x04` | `SlaveNodeFailure` | Device error while executing the request. |
| `0x06` | `SlaveDeviceBusy` | Device cannot accept the request. |
| `0x80` | `NoPermission` | Permission authentication failed or expired. |

```csharp
try
{
    SolarData data = await solar.ReadValueAsync(cancellationToken);
}
catch (FusionSolarModbusException exception)
{
    Console.Error.WriteLine(exception.Message);
    if (exception.KnownExceptionCode == FusionSolarModbusExceptionCode.NoPermission)
    {
        // Check device access/authentication; login is not implemented here.
    }
}
```

`FunctionCode` and `ExceptionCode` retain raw bytes. `KnownExceptionCode` is null
for unknown codes, which retain an explicit unknown-code description. Do not
parse messages to classify errors. Busy does not trigger a retry; callers decide
whether and when to resend. Illegal data value does not specifically establish
that an engineering value is outside the application's expected range.
Exceptions remain `IOException`s. Malformed frames, transport failures and
timeouts retain separate exception types.

## Sample models and limitations

`SolarData` contains common statuses, inverter measurements and energy counters,
including MPPT1 and meter energy. It contains no battery, voltage, current or
optimizer-count fields. Inverter power is kW; energy is kWh. Signed meter energy
does not infer import/export direction. See the [energy table](Models/SolarData.Energy.md).
Define separate models for optional meter or battery registers, and use them
only when the hardware is present.

Known status enum codes serialize as names; unknown codes remain numeric.
Multi-request snapshots are not atomic. A failed register read fails the whole
model; nullable properties do not make unsupported addresses readable.
Malformed or interrupted frames close the socket; the next operation reconnects.
There is no serial/RTU, cloud access, automatic discovery, login, optimizer-file
decoder or automatic request retry. Source inconsistencies and encoding/status
assumptions are documented in [REGISTER-SOURCES.md](REGISTER-SOURCES.md).

## Build and verify

```powershell
dotnet build Modbus.FusionSolar.Api.Console -c Release
dotnet run --project Modbus.FusionSolar.Api.Checks -c Release
```

Checks compare all 690 definitions and the bundled model mappings to the reviewed
extraction. A loopback simulator verifies selected-entry metadata resolution,
concurrent immutable caching, class/struct models, selected
writes, batching limits, gaps, types, access enforcement, scaling, sentinels,
functions 03/06/10, exceptions, reconnection, timeouts and concurrency. No inverter
is contacted and no physical write is performed.

See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and [LICENSE.md](LICENSE.md)
for upstream attribution and the retained AGPL-3.0 license.
