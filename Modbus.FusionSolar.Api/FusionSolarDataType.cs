namespace Modbus.FusionSolar.Api;

/// <summary>Wire types in the reviewed SUN2000MA map.</summary>
public enum FusionSolarDataType
{
    UInt16,
    Int16,
    /// <summary>Unsigned 32-bit integer, high word first.</summary>
    UInt32,
    /// <summary>Signed 32-bit integer, high word first.</summary>
    Int32,
    /// <summary>Enumeration code; inconsistent source word counts require raw access.</summary>
    Enum16,
    Bitfield16,
    /// <summary>32-bit flags, high word first.</summary>
    Bitfield32,
    /// <summary>Seconds since 1970-01-01, returned as an unsigned integer without timezone inference.</summary>
    EpochSeconds,
    /// <summary>Fixed-size character data, exposed as UTF-8 text or original raw words.</summary>
    String,
    /// <summary>Opaque bytes in network order.</summary>
    Bytes,
    /// <summary>Structured multiple-byte data; exposed as bytes without inferring a field layout.</summary>
    MultiData,
    /// <summary>Source permits structured data or bytes; exposed as bytes.</summary>
    MultiDataOrBytes
}
