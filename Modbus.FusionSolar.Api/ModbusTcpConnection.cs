using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Sockets;

namespace Modbus.FusionSolar.Api;

// Functions 03/06/10. All exchanges and disposal share the same connection gate.
internal sealed class ModbusTcpConnection(string host, FusionSolarConnectionOptions options) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private TcpClient? socket;
    private ushort transactionId;
    private long? lastRequestFinished;
    private bool disposed;

    public async Task<ushort[]> ReadAsync(ushort address, ushort count, CancellationToken cancellationToken)
    {
        if (count is < 1 or > 125 || address + count > 65536) throw new ArgumentOutOfRangeException(nameof(count));
        byte[] request = new byte[5];
        request[0] = 3;
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(1), address);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(3), count);
        var response = await ExchangeAsync(request, pdu =>
        {
            if (pdu.Length != 2 + count * 2 || pdu[1] != count * 2)
                throw new InvalidDataException("Invalid Modbus read-holding-registers response.");
        }, cancellationToken).ConfigureAwait(false);
        ushort[] words = new ushort[count];
        for (var index = 0; index < words.Length; index++)
            words[index] = BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(2 + index * 2));
        return words;
    }

    public async Task WriteAsync(ushort address, ReadOnlyMemory<ushort> words, CancellationToken cancellationToken)
    {
        if (words.Length is < 1 or > 123 || address + words.Length > 65536)
            throw new ArgumentOutOfRangeException(nameof(words));
        // Build the complete request before awaiting, so the caller cannot mutate a queued write.
        var single = words.Length == 1;
        byte[] request = new byte[single ? 5 : 6 + words.Length * 2];
        request[0] = single ? (byte)6 : (byte)0x10;
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(1), address);
        BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(3), single ? words.Span[0] : (ushort)words.Length);
        if (!single)
        {
            request[5] = (byte)(words.Length * 2);
            for (var index = 0; index < words.Length; index++)
                BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(6 + index * 2), words.Span[index]);
        }
        await ExchangeAsync(request, pdu =>
        {
            if (pdu.Length != 5 || !pdu.AsSpan(1).SequenceEqual(request.AsSpan(1, 4)))
                throw new InvalidDataException("Invalid Modbus write acknowledgement.");
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> ExchangeAsync(byte[] requestPdu, Action<byte[]> validate, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(options.Timeout);
            var token = timeout.Token;
            try
            {
                if (lastRequestFinished is { } finished)
                {
                    var remaining = options.WaitBetweenRequests - Stopwatch.GetElapsedTime(finished);
                    if (remaining > TimeSpan.Zero) await Task.Delay(remaining, token).ConfigureAwait(false);
                }
                if (socket is null)
                {
                    socket = new TcpClient { NoDelay = true };
                    await socket.ConnectAsync(host, options.Port, token).ConfigureAwait(false);
                    await Task.Delay(options.WaitAfterConnect, token).ConfigureAwait(false);
                }
                var id = unchecked(++transactionId);
                byte[] request = new byte[7 + requestPdu.Length];
                BinaryPrimitives.WriteUInt16BigEndian(request, id);
                BinaryPrimitives.WriteUInt16BigEndian(request.AsSpan(4), (ushort)(requestPdu.Length + 1));
                request[6] = options.UnitId;
                requestPdu.CopyTo(request, 7);
                var stream = socket.GetStream();
                await stream.WriteAsync(request, token).ConfigureAwait(false);
                byte[] header = new byte[7];
                await stream.ReadExactlyAsync(header, token).ConfigureAwait(false);
                var length = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4));
                if (BinaryPrimitives.ReadUInt16BigEndian(header) != id ||
                    BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(2)) != 0 ||
                    header[6] != options.UnitId || length is < 3 or > 254)
                    throw new InvalidDataException("Invalid Modbus TCP response header.");
                byte[] pdu = new byte[length - 1];
                await stream.ReadExactlyAsync(pdu, token).ConfigureAwait(false);
                if (pdu[0] == (requestPdu[0] | 0x80) && pdu.Length == 2)
                    throw new FusionSolarModbusException(requestPdu[0], pdu[1]);
                if (pdu[0] != requestPdu[0]) throw new InvalidDataException("Unexpected Modbus response function.");
                validate(pdu);

                return pdu;
            }
            catch (FusionSolarModbusException)
            {
                // A complete exception frame leaves the stream synchronized.
                throw;
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                Disconnect();
                throw new TimeoutException($"Modbus request to {host}:{options.Port} timed out.", exception);
            }
            catch
            {
                // Discard late/partial replies. In particular, never retry a write here.
                Disconnect();
                throw;
            }
            finally { lastRequestFinished = Stopwatch.GetTimestamp(); }
        }
        finally { gate.Release(); }
    }

    private void Disconnect()
    {
        socket?.Dispose();
        socket = null;
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try { disposed = true; Disconnect(); }
        finally { gate.Release(); }
    }
}
