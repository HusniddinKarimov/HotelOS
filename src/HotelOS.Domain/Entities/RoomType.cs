using HotelOS.Domain.Common;

namespace HotelOS.Domain.Entities;

/// <summary>A bookable room category with its nightly base rate.</summary>
public class RoomType : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Single, Double, Deluxe, Suite, Accessible
    public decimal BaseRate { get; set; }
    public int Capacity { get; set; }
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}

/// <summary>Canonical room-type names.</summary>
public static class RoomTypeNames
{
    public const string Single = "Single";
    public const string Double = "Double";
    public const string Deluxe = "Deluxe";
    public const string Suite = "Suite";
    public const string Accessible = "Accessible";

    public static readonly string[] All = { Single, Double, Deluxe, Suite, Accessible };
}
