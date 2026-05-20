namespace HotelOS.Contracts.Models;

/// <summary>
/// A guest record. Holds personal and payment data that MUST NOT be broadcast
/// over the dashboard WebSocket. Only non-sensitive fields (name, room) are
/// ever published; <see cref="MaskedCard"/> exposes a safe view of the card.
/// </summary>
public class Guest
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; init; } = string.Empty;

    public RoomType RequestedType { get; init; }
    public int? FloorPreference { get; init; }

    /// <summary>"elevator", "stairs", or null for no preference.</summary>
    public string? ProximityPreference { get; init; }

    public int Nights { get; init; } = 1;

    /// <summary>Sensitive: raw card number. Never serialised to the dashboard.</summary>
    public string? CardNumber { get; init; }

    public int? AssignedRoom { get; set; }
    public DateTime CheckInUtc { get; init; } = DateTime.UtcNow;

    /// <summary>Charges accumulated against this guest (room service, minibar...).</summary>
    public List<Charge> Charges { get; } = new();

    /// <summary>A redacted card view safe to display, e.g. "**** **** **** 1234".</summary>
    public string MaskedCard =>
        string.IsNullOrWhiteSpace(CardNumber) || CardNumber.Length < 4
            ? "—"
            : $"**** **** **** {CardNumber[^4..]}";
}

/// <summary>A single line item on a guest's bill.</summary>
public record Charge(string Description, decimal Amount, DateTime AtUtc);
