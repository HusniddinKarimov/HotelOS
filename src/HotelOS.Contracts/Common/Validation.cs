using System.Text.RegularExpressions;

namespace HotelOS.Contracts.Common;

/// <summary>
/// Raised when input fails validation. Caught by each service's error handler
/// and turned into a safe 400 response — never a raw stack trace.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>
/// Central input-validation helpers. EVERY value entering the system from
/// outside is checked here before any processing (Task 3 — Input validation).
/// </summary>
public static partial class Validation
{
    // The 10 valid rooms in the simplified hotel: 101-105 and 201-205.
    public static readonly IReadOnlySet<int> ValidRoomNumbers =
        new HashSet<int> { 101, 102, 103, 104, 105, 201, 202, 203, 204, 205 };

    [GeneratedRegex(@"^[\p{L}\p{M} .'\-]{2,60}$")]
    private static partial Regex NameRegex();

    /// <summary>A guest name must be 2-60 letters/spaces/.'- and nothing else.</summary>
    public static string RequireName(string? name)
    {
        name = name?.Trim() ?? "";
        if (!NameRegex().IsMatch(name))
            throw new ValidationException("Guest name is invalid. Use 2–60 letters only.");
        return name;
    }

    /// <summary>A room number must be one of the known rooms.</summary>
    public static int RequireRoomNumber(int roomNumber)
    {
        if (!ValidRoomNumbers.Contains(roomNumber))
            throw new ValidationException($"Room {roomNumber} does not exist. Valid rooms: 101–105, 201–205.");
        return roomNumber;
    }

    /// <summary>Nights must be between 1 and 30.</summary>
    public static int RequireNights(int nights)
    {
        if (nights is < 1 or > 30)
            throw new ValidationException("Nights must be between 1 and 30.");
        return nights;
    }

    /// <summary>A positive quantity for order items.</summary>
    public static int RequirePositiveQty(int qty)
    {
        if (qty < 1)
            throw new ValidationException("Quantity must be at least 1.");
        return qty;
    }

    /// <summary>A non-empty, reasonable-length free-text field.</summary>
    public static string RequireText(string? value, string field, int max = 200)
    {
        value = value?.Trim() ?? "";
        if (value.Length == 0)
            throw new ValidationException($"{field} is required.");
        if (value.Length > max)
            throw new ValidationException($"{field} must be {max} characters or fewer.");
        return value;
    }
}
