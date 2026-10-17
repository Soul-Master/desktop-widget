namespace Modbus.FusionSolar.Api.Models;

public class SolarProductionInfo
{
    [FusionSolarData(FusionSolarDataNames.Model)]
    public string? Model { get; set; }

    [FusionSolarData(FusionSolarDataNames.RatedPower)]
    public decimal? RatedPower { get; set; }

    [FusionSolarData(FusionSolarDataNames.TotalInputPower)]
    public decimal? TotalInputPower { get; set; }

    [FusionSolarData(FusionSolarDataNames.ThreePhaseImbalanceControl)]
    public decimal? ThreePhaseImbalanceControl { get; set; }

    [FusionSolarData(FusionSolarDataNames.ActivePowerControlMode)]
    public decimal? ActivePowerControlMode { get; set; }

    [FusionSolarData(FusionSolarDataNames.MaximumFeedGridPowerKW)]
    public decimal? MaximumFeedGridPowerKW { get; set; }
}

/// <summary>SUN2000 inverter measurements, grid power and energy counters.</summary>
/// <remarks>Inverter power is in kW; meter power is in W. Device support varies.</remarks>
public class SolarProductionData
{
    [FusionSolarData(FusionSolarDataNames.DeviceStatus)]
    public FusionSolarDeviceStatus? DeviceStatus { get; set; }

    [FusionSolarData(FusionSolarDataNames.MeterStatus)]
    public FusionSolarMeterStatus? MeterStatus { get; set; }

    [FusionSolarData(FusionSolarDataNames.ErrorCode)]
    public decimal? ErrorCode { get; set; }

    [FusionSolarData(FusionSolarDataNames.TodaySPeakActivePower)]
    public decimal? TodaySPeakActivePower { get; set; }

    /// <summary>PV DC input power in kW, before inverter conversion losses.</summary>
    [FusionSolarData(FusionSolarDataNames.TotalInputPower)]
    public double? InputPower { get; set; }

    [FusionSolarData(FusionSolarDataNames.YieldToday)]
    public double? DailyYieldEnergy { get; set; }

    /// <summary>Inverter-reported efficiency in percent; the accessor already applies the register's gain of 100.</summary>
    [FusionSolarData(FusionSolarDataNames.InverterEfficiency)]
    public double? Efficiency { get; set; }

    [FusionSolarData(FusionSolarDataNames.MeterPositiveActivePower)]
    public double? MeterPositiveActiveEnergy { get; set; }

    [FusionSolarData(FusionSolarDataNames.MeterReverseActivePower)]
    public double? MeterReverseActiveEnergy { get; set; }

    [FusionSolarData(FusionSolarDataNames.MeterCumulativeReactiveEnergy)]
    public decimal? MeterCumulativeReactiveEnergy { get; set; }

    /// <summary>Source labels this power but specifies kWh; do not use as instantaneous phase production.</summary>
    [FusionSolarData(FusionSolarDataNames.InverterPhaseAActivePower)]
    public decimal? InverterPhaseAActivePower { get; set; }

    /// <summary>Source labels this power but specifies kWh; do not use as instantaneous phase production.</summary>
    [FusionSolarData(FusionSolarDataNames.InverterPhaseBActivePower)]
    public decimal? InverterPhaseBActivePower { get; set; }

    /// <summary>Source labels this power but specifies kWh; do not use as instantaneous phase production.</summary>
    [FusionSolarData(FusionSolarDataNames.InverterPhaseCActivePower)]
    public decimal? InverterPhaseCActivePower { get; set; }

    /// <summary>Total inverter AC output in kW; may include battery discharge on hybrid systems.</summary>
    [FusionSolarData(FusionSolarDataNames.ActivePower)]
    public double? TotalProductionPower { get; set; }

    /// <summary>Signed total grid flow in W. Verify the meter's import/export sign on the installation.</summary>
    [FusionSolarData(FusionSolarDataNames.MeterActivePower)]
    public decimal? GridPower { get; set; }

    /// <summary>Signed phase A grid flow in W; not phase A household consumption.</summary>
    [FusionSolarData(FusionSolarDataNames.MeterPhaseAActivePower)]
    public decimal? PhaseAGridPower { get; set; }

    /// <summary>Signed phase B grid flow in W; not phase B household consumption.</summary>
    [FusionSolarData(FusionSolarDataNames.MeterPhaseBActivePower)]
    public decimal? PhaseBGridPower { get; set; }

    /// <summary>Signed phase C grid flow in W; not phase C household consumption.</summary>
    [FusionSolarData(FusionSolarDataNames.MeterPhaseCActivePower)]
    public decimal? PhaseCGridPower { get; set; }
}

public class SolarDataTime
{
    [FusionSolarData(FusionSolarDataNames.SystemTimeLocalTime)]
    public decimal? SystemTimeLocalTime { get; set; }
}
