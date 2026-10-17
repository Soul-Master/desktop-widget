namespace Modbus.FusionSolar.Api.Models;

/// <summary>Power values in W and efficiency in percent for a three-phase solar widget. Grid values are positive for export and negative for import.</summary>
/// <remarks>Phase values are net grid flows, not phase production or household consumption.
/// A multi-request reading is not an atomic snapshot. Values can reflect different sample times.
/// Total net export need not equal the sum of phase exports when phases flow in different directions.</remarks>
public sealed record SolarPowerSnapshot
{
    /// <summary>PV DC input in W, from TotalInputPower (32064). Null if unavailable or negative.</summary>
    public double? PvProductionW { get; init; }

    private double? ProductionW { get; init; }
    /// <summary>Explicit AC output name for ProductionW. Household consumption uses this value, not PV DC input.</summary>
    public double? InverterOutputW => ProductionW;

    /// <summary>Inverter-reported efficiency in percent (0 to 100), from InverterEfficiency (32086).</summary>
    public double? EfficiencyPercent { get; init; }

    /// <summary>Inverter AC output minus signed total grid export, in W. Null if unavailable or negative.</summary>
    /// <remarks>Negative results indicate inconsistent sampling or an unaccounted source/configuration; they are not clamped to zero.</remarks>
    public double? ConsumptionW { get; init; }

    /// <summary>Signed total net grid flow in W: positive export, negative import.</summary>
    public double? GridW { get; init; }

    /// <summary>Signed phase A (L1) net grid flow in W: positive export, negative import.</summary>
    public double? PhaseAGridW { get; init; }

    /// <summary>Signed phase B (L2) net grid flow in W: positive export, negative import.</summary>
    public double? PhaseBGridW { get; init; }

    /// <summary>Signed phase C (L3) net grid flow in W: positive export, negative import.</summary>
    public double? PhaseCGridW { get; init; }

    /// <summary>True only when the device reports a normal meter status.</summary>
    public bool IsMeterOnline { get; init; }

    /// <summary>Total net export in W, or null if the total grid reading is unavailable.</summary>
    public double? GridExportW => Export(GridW);

    /// <summary>Total net import in W, or null if the total grid reading is unavailable.</summary>
    public double? GridImportW => Import(GridW);

    /// <summary>Phase A export in W, or null if unavailable.</summary>
    public double? PhaseAExportW => Export(PhaseAGridW);

    /// <summary>Phase B export in W, or null if unavailable.</summary>
    public double? PhaseBExportW => Export(PhaseBGridW);

    /// <summary>Phase C export in W, or null if unavailable.</summary>
    public double? PhaseCExportW => Export(PhaseCGridW);

    /// <summary>Phase A import in W, or null if unavailable.</summary>
    public double? PhaseAImportW => Import(PhaseAGridW);

    /// <summary>Phase B import in W, or null if unavailable.</summary>
    public double? PhaseBImportW => Import(PhaseBGridW);

    /// <summary>Phase C import in W, or null if unavailable.</summary>
    public double? PhaseCImportW => Import(PhaseCGridW);

    /// <summary>Normalizes a model reading without I/O. No sign convention or equal phase output is assumed.</summary>
    /// <param name="data">The raw register reading.</param>
    /// <param name="meterPositiveMeansExport">True for export-positive meters; false for import-positive meters.</param>
    public static SolarPowerSnapshot From(SolarProductionData data, bool meterPositiveMeansExport)
    {
        ArgumentNullException.ThrowIfNull(data);
        var meterOnline = data.MeterStatus == FusionSolarMeterStatus.Normal;
        var production = Finite(data.TotalProductionPower * 1000);
        var pvProduction = Finite(data.InputPower * 1000);
        var efficiency = Finite(data.Efficiency);
        var sign = meterPositiveMeansExport ? 1 : -1;
        double? Grid(decimal? raw) => meterOnline && raw.HasValue ? (double)raw.Value * sign : null;
        var grid = Grid(data.GridPower);
        var consumption = Finite(production - grid);
        return new()
        {
            ProductionW = production,
            PvProductionW = pvProduction >= 0 ? pvProduction : null,
            EfficiencyPercent = efficiency is >= 0 and <= 100 ? efficiency : null,
            ConsumptionW = consumption >= 0 ? consumption : null,
            GridW = grid,
            PhaseAGridW = Grid(data.PhaseAGridPower),
            PhaseBGridW = Grid(data.PhaseBGridPower),
            PhaseCGridW = Grid(data.PhaseCGridPower),
            IsMeterOnline = meterOnline
        };
    }

    private static double? Finite(double? value) => value is { } number && double.IsFinite(number) ? number : null;
    private static double? Export(double? value) => value is { } watts ? Math.Max(watts, 0) : null;
    private static double? Import(double? value) => value is { } watts ? Math.Max(-watts, 0) : null;
}
