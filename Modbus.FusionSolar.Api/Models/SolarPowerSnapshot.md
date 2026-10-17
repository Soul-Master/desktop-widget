# Three-phase widget data

Read the existing model, then explicitly select the installation's meter sign:

```csharp
var raw = await accessor.ReadValueAsync(cancellationToken);
var power = SolarPowerSnapshot.From(raw, meterPositiveMeansExport: true);
```

Use `false` if positive raw meter values mean import. Verify direction with a
known import/export condition before selecting it. The conversion performs no I/O.

| Widget element | Snapshot property | Register or calculation |
| --- | --- | --- |
| Production total | `ProductionW` | `TotalProductionPower` / `ActivePower`, 32080, kW multiplied by 1000 |
| Explicit inverter AC output | `InverterOutputW` | Same value as `ProductionW`; used for consumption calculations |
| PV DC production | `PvProductionW` | `InputPower` / `TotalInputPower`, 32064, kW multiplied by 1000 |
| Reported inverter efficiency | `EfficiencyPercent` | `Efficiency` / `InverterEfficiency`, 32086; already scaled to percent |
| Calculated conversion efficiency | `CalculatedEfficiencyPercent` | `InverterOutputW / PvProductionW * 100`, without battery power flows |
| Estimated conversion loss | `ConversionLossW` | `PvProductionW - InverterOutputW`, without battery power flows |
| Usage total | `ConsumptionW` | `ProductionW - GridW` |
| Grid total | `GridW` | `GridPower` / `MeterActivePower`, 37113, W, normalized sign |
| L1 signed grid bar | `PhaseAGridW` | `PhaseAGridPower` / `MeterPhaseAActivePower`, 37132, W, normalized sign |
| L2 signed grid bar | `PhaseBGridW` | `PhaseBGridPower` / `MeterPhaseBActivePower`, 37134, W, normalized sign |
| L3 signed grid bar | `PhaseCGridW` | `PhaseCGridPower` / `MeterPhaseCActivePower`, 37136, W, normalized sign |

All snapshot power values are W. Divide by 1000 only for a kW display.
Efficiency values are percentages (for example, 95 means 95%, not 0.95).
Positive grid values mean export; negative values mean import. Optional
`GridExportW`, `GridImportW`, and `PhaseA/B/CExportW` / `PhaseA/B/CImportW`
properties separate the two directions. Total import/export is net across
phases; it can differ from the sum of the separately clipped phase values.

The reference card's example is:

```json
{
  "productionW": 3800,
  "consumptionW": 2500,
  "gridW": 1300,
  "phaseAGridW": 850,
  "phaseBGridW": -400,
  "phaseCGridW": 850
}
```

## Efficiency without a battery

With 4000 W of PV DC input and 3800 W of inverter AC output:

```text
CalculatedEfficiencyPercent = 3800 / 4000 * 100 = 95%
ConversionLossW = 4000 - 3800 = 200 W
```

Use `EfficiencyPercent` for the inverter-reported reading. Keep the calculated
estimate separate: the device's measurement timing or calculation can differ.
Neither value describes solar panel sunlight-to-electricity efficiency.

`CalculatedEfficiencyPercent` is null when PV input is zero or missing, AC output
is negative or missing, or AC output exceeds PV input. `ConversionLossW` is null
for missing/negative powers or AC output exceeding PV input. A genuine zero
input/output pair has zero estimated loss, but no defined calculated efficiency.
Invalid results are not clamped to 100% or zero loss. Small input values can make
the calculated percentage unstable, even when it falls between 0 and 100.

## Assumptions and unavailable values

- The meter must cover the property's grid connection. Consumption assumes all
  AC sources and storage are accounted for by the measured inverter and meter.
- `ProductionW` is inverter AC output, not solar DC input. On a hybrid inverter
  it can include battery power. `PvProductionW` exposes PV DC input separately.
  The calculated efficiency/loss assume no battery power flows; if a battery is
  later added, do not treat these calculations as conversion efficiency/loss.
- Negative/non-finite PV DC input and reported efficiencies outside 0 to 100%
  are exposed as null. These values do not depend on meter availability.
- Phase bars show measured grid flow. They work with asymmetric inverter output
  and do not imply measured phase production or household consumption.
- Exact phase production/consumption still needs verified phase inverter power.
  Registers 32133/32135/32137 have conflicting power labels and kWh metadata;
  this conversion does not use them or divide total output by three.
- Unless `MeterStatus` is `Normal`, all grid values and calculated consumption
  are null. A missing individual field stays null, not zero. A normal status
  alone does not guarantee each field is present or fresh.
- Non-finite production and negative calculated consumption return null. A
  negative calculation can result from sample timing, incorrect sign, meter
  placement, or unaccounted sources; it should not be displayed as zero usage.
- Register batches are read sequentially, so this is not an atomic snapshot.
  Track read start/completion and failures separately to identify stale values.

This prepares data only; it does not create a widget. Serializing the snapshot
includes these added efficiency fields.
