# FusionSolar console monitor

Continuously reads measurements through `Modbus.FusionSolar.Api`. After each
successful read, it clears the terminal and prints the latest formatted JSON.
Press Ctrl+C to stop and close the connection.

The monitor constructs `FusionSolarDataAccessor<SolarData>(client)` once and
polls it with `ReadValueAsync(cancellationToken)`. Model metadata and read plans
are cached; monitoring does not initialize the full register catalog. The client
owns the connection and is disposed when monitoring stops.

In Visual Studio, select `Modbus.FusionSolar.Api.Console` as the startup project
and use its launch profile. The profile passes `192.168.68.55 --port 502 --unit-id 1`,
matching the working Python example's unit ID. Unit ID 1 is also the library and
console default; `--unit-id` can override it for other installations.
Edit `Properties/launchSettings.json` to change these launch arguments.

```powershell
dotnet run --project Modbus.FusionSolar.Api.Console -- 192.168.68.55 --port 502 --unit-id 1
dotnet run --project Modbus.FusionSolar.Api.Console -- 192.168.1.100 --port 502 --unit-id 1 --interval 2 --meter --battery
```

Replace the example host, port and unit ID with your inverter settings. Defaults
are port 502 and unit ID 1, matching the library. `--interval` sets the delay in
seconds after each read attempt (default 5, allowed range 1–3600). Reads do not
overlap, so the actual refresh period also includes the time spent reading.

`SolarData` is always read and includes selected non-storage SUN2000 energy
registers in kWh, including inverter, MPPT1 and grid energy totals. It contains no
voltage, current, storage or battery properties. Some energy registers require
particular firmware, MPPT inputs or a meter; unsupported addresses fail the snapshot. Use a smaller custom
model for installations that cannot read the comprehensive model. `--meter` adds
instantaneous `GridMeterData` power readings; `--battery` adds `BatteryData` charge
level and power. `--meter` does not control grid energy fields already in `SolarData`;
leave `--battery` off for installations without storage.

The JSON `solar` object also contains `deviceStatus` and `meterStatus`.
Known statuses appear as strings, for
example `"OnGrid"` and `"Normal"`; unknown codes remain numeric and unavailable
values are null. Optimizer counts are omitted because the reviewed SPC106
document does not list their registers. An offline meter does not imply
a utility outage, and standby at night is not necessarily a fault.

JSON contains `observedAt` (UTC time when the read finished), `solar`, `gridMeter`
and `battery`, with camelCase properties. Unrequested sections and unavailable
measurements are `null`. Units are documented in the
[API README](../Modbus.FusionSolar.Api/README.md). Multi-request snapshots are not
atomic and signed values retain the device's sign.

Inverter `inputPower` and `activePower` are now **kW**, following the reviewed
document. Meter instantaneous power remains W. Meter energy uses
`meterPositiveActiveEnergy` and `meterReverseActiveEnergy` in kWh and retains the
reported sign; the former `gridExportedEnergy` and `gridAccumulatedEnergy` names
are removed. The console only reads data; it never calls the write API.

Read failures are written to stderr and retried after the configured interval.
The last successful JSON stays visible during a failure; its `observedAt` timestamp
does not advance. Each snapshot has a 30-second overall timeout in addition to
the library's 10-second per-request timeout. Invalid arguments print usage and exit
with code 2. Ctrl+C stops monitoring with exit code 0.

When stdout is redirected, clearing is disabled and each snapshot is written as
one compact JSON line (NDJSON). Build first to keep build messages out of the stream:

```powershell
dotnet build Modbus.FusionSolar.Api.Console -c Release
& ./Modbus.FusionSolar.Api.Console/bin/Release/net10.0/Modbus.FusionSolar.Api.Console.exe 192.168.1.100 --meter > solar-data.jsonl
```

Requires .NET 10 to run from source and local Modbus TCP access to the inverter.
This is polling, not a push subscription. Hardware compatibility depends on the
selected registers, device model and firmware.
