using System.Text.Json;
using System.Text.Json.Serialization;

namespace HotelOS.Contracts.Messaging;

/// <summary>
/// The wire format exchanged with the broker. A client sends one of these to
/// subscribe or publish; the broker forwards published messages (with the
/// <see cref="Payload"/> intact) to every subscriber of <see cref="Topic"/>.
/// </summary>
public class BrokerMessage
{
    /// <summary>"subscribe" or "publish".</summary>
    public string Action { get; set; } = "publish";

    public string Topic { get; set; } = string.Empty;

    /// <summary>JSON-encoded event body (only present for "publish").</summary>
    public string? Payload { get; set; }

    /// <summary>Deserialise the payload into a strongly-typed event.</summary>
    public T? PayloadAs<T>() =>
        Payload is null ? default : JsonSerializer.Deserialize<T>(Payload, Json.Options);

    public static BrokerMessage Publish<T>(string topic, T body) => new()
    {
        Action = "publish",
        Topic = topic,
        Payload = JsonSerializer.Serialize(body, Json.Options)
    };

    public static BrokerMessage Subscribe(string topic) => new()
    {
        Action = "subscribe",
        Topic = topic
    };
}

/// <summary>Shared JSON settings so every service serialises identically.</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new()
    {
        // camelCase so payloads pushed over the WebSocket match the browser JS
        // (r.number, r.status ...). All C# clients share these options, so the
        // broker round-trip stays internally consistent too.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        // Serialise enums as readable strings ("Clean", "Critical") so both the
        // broker payloads and the service REST responses are UI-friendly.
        Converters = { new JsonStringEnumConverter() }
    };
}
