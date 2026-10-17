namespace Modbus.FusionSolar.Api;

/// <summary>Minimal value-type model demonstrating a compile-time register name.</summary>
public struct FusionSolarData
{
    /// <summary>Inverter active output power, in kW.</summary>
    [SolarData(FusionSolarDataNames.ActivePower)]
    public double? InverterPower { get; set; }
}
