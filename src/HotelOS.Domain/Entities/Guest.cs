using HotelOS.Domain.Common;

namespace HotelOS.Domain.Entities;

/// <summary>A hotel guest. Holds personal data; never exposed raw over SignalR.</summary>
public class Guest : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Nationality { get; set; }
    public string? PassportNumber { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
