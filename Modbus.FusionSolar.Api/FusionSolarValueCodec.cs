using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace Modbus.FusionSolar.Api;

internal static class FusionSolarValueCodec
{
    private static readonly UTF8Encoding TextEncoding = new(false, true);

    public static bool IsScalar(FusionSolarRegisterDefinition register) => register.DataType switch
    {
        FusionSolarDataType.UInt16 or FusionSolarDataType.Int16 or FusionSolarDataType.Enum16 or
            FusionSolarDataType.Bitfield16 => register.WordCount == 1,
        FusionSolarDataType.UInt32 or FusionSolarDataType.Int32 or FusionSolarDataType.Bitfield32 or
            FusionSolarDataType.EpochSeconds => register.WordCount == 2,
        _ => false
    };

    public static object? Decode(FusionSolarRegisterDefinition register, ReadOnlySpan<ushort> words)
    {
        if (words.Length != register.WordCount) throw new ArgumentException("Incorrect register word count.", nameof(words));
        if (!IsScalar(register))
        {
            var bytes = ToBytes(words);
            return register.DataType == FusionSolarDataType.String
                ? TextEncoding.GetString(bytes).TrimEnd('\0') : bytes;
        }

        var unsigned = register.WordCount == 1 ? words[0] : ((uint)words[0] << 16) | words[1];
        // Invalid numeric sentinels retain the original Python library's convention.
        object raw;
        switch (register.DataType)
        {
            case FusionSolarDataType.Int16:
                if (unsigned == short.MaxValue) return null;
                raw = unchecked((short)unsigned);
                break;
            case FusionSolarDataType.Int32:
                if (unsigned == int.MaxValue) return null;
                raw = unchecked((int)unsigned);
                break;
            default:
                if (unsigned == (register.WordCount == 1 ? ushort.MaxValue : uint.MaxValue)) return null;
                raw = register.WordCount == 1 ? (object)(ushort)unsigned : unsigned;
                break;
        }
        return register.Gain is > 1 ? Convert.ToDouble(raw, CultureInfo.InvariantCulture) / register.Gain.Value : raw;
    }

    public static ushort[] Encode(FusionSolarRegisterDefinition register, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!IsScalar(register))
        {
            byte[] bytes;
            if (register.DataType == FusionSolarDataType.String)
            {
                if (value is not string text || text.Contains('\0'))
                    throw new ArgumentException("A string value without embedded NUL characters is required.", nameof(value));
                var encoded = TextEncoding.GetBytes(text);
                if (encoded.Length > register.WordCount * 2) throw new ArgumentOutOfRangeException(nameof(value), "Encoded string is too long.");
                bytes = new byte[register.WordCount * 2];
                encoded.CopyTo(bytes, 0);
            }
            else
            {
                if (value is not byte[] data || data.Length != register.WordCount * 2)
                    throw new ArgumentException("An exact-size byte array is required for this structured or ambiguous register.", nameof(value));
                bytes = data;
            }
            return FromBytes(bytes);
        }

        if (!IsNumericType(value.GetType()) && !value.GetType().IsEnum)
            throw new ArgumentException("A numeric value or enum code is required.", nameof(value));
        var raw = Convert.ToDecimal(value, CultureInfo.InvariantCulture) * (register.Gain ?? 1);
        if (raw != decimal.Truncate(raw))
            throw new ArgumentException("Value cannot be represented exactly at the register's gain.", nameof(value));
        uint bits;
        switch (register.DataType)
        {
            case FusionSolarDataType.Int16:
                var signed16 = checked((short)raw);
                if (signed16 == short.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), "Value is the unavailable sentinel.");
                bits = unchecked((ushort)signed16);
                break;
            case FusionSolarDataType.Int32:
                var signed32 = checked((int)raw);
                if (signed32 == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(value), "Value is the unavailable sentinel.");
                bits = unchecked((uint)signed32);
                break;
            default:
                bits = register.WordCount == 1 ? checked((ushort)raw) : checked((uint)raw);
                if (bits == (register.WordCount == 1 ? ushort.MaxValue : uint.MaxValue))
                    throw new ArgumentOutOfRangeException(nameof(value), "Value is the unavailable sentinel.");
                break;
        }
        return register.WordCount == 1 ? [(ushort)bits] : [(ushort)(bits >> 16), (ushort)bits];
    }

    public static object? ConvertValue(object? value, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (value is null)
        {
            if (targetType.IsValueType && underlying is null)
                throw new InvalidDataException("The register is unavailable; use a nullable property or result type.");
            return null;
        }
        var type = underlying ?? targetType;
        if (type.IsInstanceOfType(value)) return value;
        if (type.IsEnum)
        {
            var converted = ConvertValue(value, Enum.GetUnderlyingType(type))!;
            return Enum.ToObject(type, converted);
        }
        if (!IsNumericType(type) || !IsNumericType(value.GetType()))
            throw new InvalidDataException($"Register value of type {value.GetType().Name} cannot map to {type.Name}.");
        if (type != typeof(double) && type != typeof(float) && type != typeof(decimal) &&
            Convert.ToDecimal(value, CultureInfo.InvariantCulture) != decimal.Truncate(Convert.ToDecimal(value, CultureInfo.InvariantCulture)))
            throw new InvalidDataException("A fractional register value cannot map to an integer or status.");
        return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }

    public static bool IsNumericType(Type type) => Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or
        TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
        TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    public static byte[] ToBytes(ReadOnlySpan<ushort> words)
    {
        byte[] bytes = new byte[words.Length * 2];
        for (var index = 0; index < words.Length; index++) BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(index * 2), words[index]);
        return bytes;
    }

    private static ushort[] FromBytes(ReadOnlySpan<byte> bytes)
    {
        ushort[] words = new ushort[bytes.Length / 2];
        for (var index = 0; index < words.Length; index++) words[index] = BinaryPrimitives.ReadUInt16BigEndian(bytes[(index * 2)..]);
        return words;
    }
}
