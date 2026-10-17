namespace Modbus.FusionSolar.Api;

/// <summary>Short form for an enum-based FusionSolar property mapping.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class SolarDataAttribute(FusionSolarDataNames name) : FusionSolarDataAttribute(name);
