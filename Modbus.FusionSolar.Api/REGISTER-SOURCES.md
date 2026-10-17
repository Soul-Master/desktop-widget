# Register documentation source

The catalog is ported from the user-supplied Huawei document:

**SUN2000MA V200R024C00SPC106, Modbus Interface Definitions(V3.0),
Issue 06 (2025-06-12)**, Huawei Digital Power Technologies Co., Ltd.

Source PDF SHA-256:
`df3bc623f1dc8a33b14387e66d797b5764d397de4a8efea9d4db0b92ec4d5310`

The reviewed extraction contains all **690 register definitions**: 522 inverter
and 168 battery entries. Access modes are 410 RO, 255 RW and 25 WO. The review
workbook also includes alarm bit definitions, grid codes and structured field
descriptions. Those supplementary tables do not introduce extra register names.
The original PDF is not redistributed in this project.

## Catalog and documentation

`FusionSolarDataNames : ushort` contains compile-time register names and addresses.
Bracketed tags become underscore-separated prefixes:
`[Energy storage unit 1] [Battery pack 1] SOC` becomes
`FusionSolarDataNames.EnergyStorageUnit1_BatteryPack1_StateOfCapacity`.
Members without leading tags have no group prefix. Meter names use a `Meter` prefix
to distinguish them from inverter readings; duplicate settings are distinguished
by their units or documented broadcast role.

XML summaries that only repeat a register or group name are omitted. Clearer equivalent scope
descriptions replace short labels in summaries; they are not duplicated in
remarks. Other scope/description text appears first in remarks without a `Scope:` prefix.
The comments omit source-page numbers, the address/size/type/access sentence,
and repeated unit/gain declarations and scaling instructions.
Those technical details live in the enum values, metadata attributes and access markers:

- `[SolarDataNameMetadata]` supplies address, wire type, word count, gain and unit.
  Its address must match the enum value. Unspecified gain remains zero in the
  attribute and null in the resolved register definition.
- No access marker means **read-only**.
- `[FusionSolarWrite]` means **read/write**.
- `[FusionSolarWriteOnly]` means **write-only**.

Abbreviations are expanded where the supplied descriptions establish their
meaning: SN to SerialNumber, PN to ProductCode, ID to Identifier, SOC to
StateOfCapacity, TOU to TimeOfUse and LVRT to LowVoltageRideThrough. The source
specifically describes battery SOC as state of capacity. Hardware acronyms and
symbolic curve quantities without a supplied expansion are retained.

`FusionSolarDataAccessor<T>` accepts classes and structs with enum-based
`[SolarData(name)]` or `[FusionSolarData(name)]` property attributes. The enum
reference is defined at compile time. Requested members' metadata is reflected,
validated, and cached at runtime after named attribute arguments have been applied.
Attribute constructors do not register metadata. `FusionSolarNameMetadata.Get(name)`
resolves one entry; its shared `Mapping` is a read-only view of immutable snapshots.
No full-catalog metadata loader runs during model startup. Enum properties inherit
documented access capabilities and cannot override wire metadata or enable writes
to RO registers. A property may narrow RW access to WO.
The verification project compares all enum definitions and bundled mappings with
the reviewed source. Explicit register definitions supplied to the non-generic
write accessor are not cross-checked against the catalog during operations.
The client owns the connection and transport; accessors perform mapped reads,
decoding, explicit writes and encoding through the client supplied to their
constructor. `ReadValueAsync()` reads a whole mapped model; single-register read
methods are not exposed.
Missing gains remain null in metadata. Unscaled numeric APIs return raw numbers
when no gain is specified; this does not infer an engineering-unit conversion.
Visual Studio consumes the generated XML documentation beside the DLL, also
included in packages.

## Device exception codes

Section 6.3.2, Table 6-4 (printed pages 138-139), defines six codes:
`0x01` illegal function, `0x02` illegal data address, `0x03` illegal data value,
`0x04` slave node failure, `0x06` slave device busy and `0x80` no permission.
`FusionSolarModbusExceptionCode` follows this list. The existing
`FusionSolarModbusException` preserves the raw bytes and adds a nullable
`KnownExceptionCode`, a readable `Description` and an expanded message.
Unknown codes are not assigned a documented meaning.

The transport already recognized exception responses for functions 03/06/10;
this addition supplies their meaning without changing frame handling, connection
reuse or retry policy. Section 6.3.2 leaves busy retry decisions to the caller.
The ten-second/six-resend rule in section 6.3.7.1.1 concerns starting a file
upload (function 0x41), which this client does not implement. It is not applied
to register reads or writes. No permission describes failed or expired
authentication; adding the code does not add login support.

The bundled Huawei Solar 3.0.7 Python implementation separately registers
`PermissionDeniedError` for `0x80` and exposes device-busy/device-failure retry
options. These Python behaviors are not C# transport implementations or a
reason to enable implicit retries here. Loopback checks cover all six listed
codes plus unknown codes for each supported function, preserving the socket and
verifying that failed operations are not automatically retried.

## Migration from the Python catalog

The older 744-member enum covered several device families. It is replaced by
this document-specific catalog, with no legacy aliases. Names whose addresses
are absent from the document are removed, including optimizer counts at
37200/37201 and MPPT5-10 cumulative energy at 32220-32230. A shared address does
not preserve another product family's meaning: each definition now follows the
reviewed SUN2000MA table.

The sample's inverter input/output power now uses **kW**, gain 1000. Meter
instantaneous power remains **W**. Meter energy preserves its sign rather than
applying the former Python absolute-value transformation. Properties formerly
named `GridExportedEnergy` and `GridAccumulatedEnergy` are now
`MeterPositiveActiveEnergy` and `MeterReverseActiveEnergy`; the PDF does not
establish the installation's import/export direction.

## Source limitations

- Some signal names say power while their documented unit is kWh, including
  inverter phase energy entries and meter positive/reverse energy. The supplied
  unit is retained and the inconsistency is called out in the comments.
- Six entries label data Enum16 but allocate two or ten words. They are exposed
  as opaque bytes or raw words; scalar interpretation is not inferred.
- MLD/bytes structures remain opaque. A reference to a separate key-signal
  description does not provide that description. The 43-word time-of-use entry
  does not establish the layout of the 41-word examples.
- String conversion uses strict UTF-8 with trailing NUL padding. The supplied
  table does not specify text encoding; raw access preserves the original bytes.
- Numeric unavailable markers and the two status enums retain behavior from
  Huawei Solar 3.0.7. The PDF does not supply every status or invalid-value rule.
- RW/WO indicates documented capability. Firmware permissions, installer access,
  model options and parameter prerequisites can still prevent a write. Human
  scope notes are not an automatic range validator. Writes are explicit and are
  never retried automatically; a lost acknowledgement leaves the outcome unknown.
- Catalog availability does not prove support on a connected inverter. Loopback
  verification contacts no physical inverter and performs no physical writes.

The source expectations used for verification are stored in
`../Modbus.FusionSolar.Api.Checks/Registers.SPC106.json`.
