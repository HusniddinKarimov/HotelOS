using HotelOS.Contracts.Models;

namespace HotelOS.Reception.Domain;

/// <summary>One itemised line on the final bill.</summary>
public record BillLine(string Description, decimal Amount);

/// <summary>The fully-computed bill returned to Reception on checkout.</summary>
public record Bill(
    int RoomNumber,
    string GuestName,
    int Nights,
    IReadOnlyList<BillLine> Lines,
    decimal Subtotal,
    decimal Discount,
    decimal Total);

/// <summary>
/// PROCEDURAL billing routine: a sequence of clear steps that operate on the
/// guest + room data. Handles the required edge cases — early checkout
/// (nights clamped to at least 1), zero charges, and discount application.
/// </summary>
public static class BillingService
{
    /// <param name="discountRate">0.0–1.0 fraction, e.g. 0.10 for 10% off.</param>
    public static Bill Calculate(Room room, Guest guest, decimal discountRate = 0m)
    {
        var lines = new List<BillLine>();

        // Step 1: room cost. Early checkout still bills a minimum of one night.
        var nights = Math.Max(1, guest.Nights);
        var roomCost = room.NightlyRate * nights;
        lines.Add(new BillLine($"Room {room.Number} ({room.Type}) — {nights} night(s) @ £{room.NightlyRate}", roomCost));

        // Step 2: every charge logged against the stay (room service, minibar...).
        // If there are zero charges this loop simply adds nothing.
        foreach (var charge in guest.Charges)
            lines.Add(new BillLine(charge.Description, charge.Amount));

        // Step 3: subtotal.
        var subtotal = lines.Sum(l => l.Amount);

        // Step 4: discount (clamped to a sane range to avoid negative bills).
        var rate = Math.Clamp(discountRate, 0m, 1m);
        var discount = Math.Round(subtotal * rate, 2);

        // Step 5: total.
        var total = subtotal - discount;

        return new Bill(room.Number, guest.Name, nights, lines, subtotal, discount, total);
    }
}
