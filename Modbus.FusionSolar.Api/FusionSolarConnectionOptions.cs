namespace Modbus.FusionSolar.Api;

/// <summary>Modbus TCP settings. Unit ID 1 matches the bundled SUN2000 monitoring example.</summary>
public sealed record FusionSolarConnectionOptions
{
    public int Port { get; init; } = 502;
    public byte UnitId { get; init; } = 1;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan WaitAfterConnect { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan WaitBetweenRequests { get; init; } = TimeSpan.FromMilliseconds(50);

    internal void Validate()
    {
        if (Port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(Port));
        ValidateDelay(Timeout, nameof(Timeout), allowZero: false);
        ValidateDelay(WaitAfterConnect, nameof(WaitAfterConnect), allowZero: true);
        ValidateDelay(WaitBetweenRequests, nameof(WaitBetweenRequests), allowZero: true);
    }

    private static void ValidateDelay(TimeSpan value, string name, bool allowZero)
    {
        if (value < TimeSpan.Zero || (!allowZero && value == TimeSpan.Zero) ||
            value.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(name);
    }
}
