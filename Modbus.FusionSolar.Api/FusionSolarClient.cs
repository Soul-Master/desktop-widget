namespace Modbus.FusionSolar.Api;

/// <summary>Owns a reusable Modbus TCP connection. Supply it when constructing a data accessor for reads and writes.</summary>
public sealed class FusionSolarClient : IAsyncDisposable
{
    private readonly ModbusTcpConnection connection;

    /// <summary>Creates a client. TCP connects lazily on the first accessor operation.</summary>
    public FusionSolarClient(string host, FusionSolarConnectionOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        options ??= new();
        options.Validate();
        connection = new(host, options);
    }

    internal Task<ushort[]> ReadHoldingRegistersAsync(ushort address, ushort count, CancellationToken cancellationToken) =>
        connection.ReadAsync(address, count, cancellationToken);

    internal Task WriteRegistersAsync(ushort address, ReadOnlyMemory<ushort> words, CancellationToken cancellationToken) =>
        connection.WriteAsync(address, words, cancellationToken);

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}
