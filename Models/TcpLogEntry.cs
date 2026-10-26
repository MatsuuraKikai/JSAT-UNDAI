using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JSAT_UNDAI.Models;

public enum TcpLogType
{
    RX,          // data received from the remote endpoint
    TX,          // data sent to the remote endpoint
    Connect,     // connection established
    Disconnect,  // connection closed
    Error        // socket error, parse error, dropped-entry report
}

public sealed class TcpLogEntry
{
    [JsonConverter(typeof(LocalTimestampConverter))]
    public DateTimeOffset Timestamp { get; init;}

    public TcpLogType Type { get; init;}

    /// <summary>Remote endpoint, e.g. "192.168.1.50:5000".</summary>
    public string Endpoint { get; init; } = string.Empty;

    public int Length { get; init; }

    public string Message { get; init; } = string.Empty;
}

public sealed class LocalTimestampConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss.fff";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.ParseExact(
            reader.GetString()!, Format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToLocalTime().ToString(Format, CultureInfo.InvariantCulture));
    }
}

