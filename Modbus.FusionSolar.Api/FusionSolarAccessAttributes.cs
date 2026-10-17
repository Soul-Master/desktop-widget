namespace Modbus.FusionSolar.Api;

/// <summary>Marks a register or mapped property readable and writable. No access attribute means read-only.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FusionSolarWriteAttribute : Attribute;

/// <summary>Marks a register or mapped property writable only; reads are rejected before network access.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FusionSolarWriteOnlyAttribute : Attribute;
