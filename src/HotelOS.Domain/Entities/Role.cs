using HotelOS.Domain.Common;

namespace HotelOS.Domain.Entities;

/// <summary>An RBAC role (Administrator, Receptionist, Housekeeping, etc.).</summary>
public class Role : IAuditable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

/// <summary>Canonical role names used across the system and for [Authorize(Roles=...)].</summary>
public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string HotelManager = "HotelManager";
    public const string Receptionist = "Receptionist";
    public const string Housekeeping = "Housekeeping";
    public const string KitchenStaff = "KitchenStaff";
    public const string RoomServiceStaff = "RoomServiceStaff";
    public const string MaintenanceStaff = "MaintenanceStaff";

    /// <summary>Basic user: can sign in and view, but performs no staff operations.</summary>
    public const string User = "User";

    public static readonly string[] All =
    {
        Administrator, HotelManager, Receptionist, Housekeeping,
        KitchenStaff, RoomServiceStaff, MaintenanceStaff, User
    };
}
