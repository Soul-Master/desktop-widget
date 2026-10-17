using System.Text.Json.Serialization;

namespace Modbus.FusionSolar.Api;

/// <summary>Attached meter states from Huawei Solar 3.0.7 register_values.py (AGPL-3.0).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FusionSolarMeterStatus>))]
public enum FusionSolarMeterStatus : ushort
{
    Offline = 0,
    Normal = 1
}
