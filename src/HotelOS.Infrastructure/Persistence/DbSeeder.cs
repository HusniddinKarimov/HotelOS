using HotelOS.Application.Abstractions;
using HotelOS.Domain.Entities;
using HotelOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Infrastructure.Persistence;

/// <summary>Applies migrations and seeds reference data, demo users and rooms.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        await SeedRolesAsync(db, ct);
        await SeedRoomTypesAsync(db, ct);
        await SeedUsersAsync(db, hasher, ct);
        await SeedRoomsAsync(db, ct);
    }

    private static async Task SeedRolesAsync(AppDbContext db, CancellationToken ct)
    {
        foreach (var name in RoleNames.All)
            if (!await db.Roles.AnyAsync(r => r.Name == name, ct))
                db.Roles.Add(new Role { Name = name, Description = $"{name} role" });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRoomTypesAsync(AppDbContext db, CancellationToken ct)
    {
        var defaults = new (string Name, decimal Rate, int Cap)[]
        {
            (RoomTypeNames.Single, 80m, 1),
            (RoomTypeNames.Double, 120m, 2),
            (RoomTypeNames.Deluxe, 180m, 2),
            (RoomTypeNames.Suite, 250m, 4),
            (RoomTypeNames.Accessible, 110m, 2)
        };
        foreach (var (name, rate, cap) in defaults)
            if (!await db.RoomTypes.AnyAsync(t => t.Name == name, ct))
                db.RoomTypes.Add(new RoomType { Name = name, BaseRate = rate, Capacity = cap, Description = $"{name} room" });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedUsersAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        var roles = await db.Roles.ToDictionaryAsync(r => r.Name, r => r.Id, ct);

        // Staff accounts are seeded; customers self-register (sign up) as basic Users.
        // (username, fullName, role, password)
        var users = new (string U, string Full, string Role, string Pwd)[]
        {
            ("admin", "System Administrator", RoleNames.Administrator, "Admin@123"),
            ("manager", "Hotel Manager", RoleNames.HotelManager, "Password@123"),
            ("reception", "Front Desk", RoleNames.Receptionist, "Password@123"),
            ("housekeeping", "House Keeper", RoleNames.Housekeeping, "Password@123"),
            ("kitchen", "Kitchen Staff", RoleNames.KitchenStaff, "Password@123"),
            ("roomservice", "Room Service", RoleNames.RoomServiceStaff, "Password@123"),
            ("maintenance", "Technician", RoleNames.MaintenanceStaff, "Password@123"),
        };

        foreach (var (u, full, role, pwd) in users)
            if (!await db.Users.AnyAsync(x => x.Username == u, ct))
                db.Users.Add(new User
                {
                    Username = u,
                    Email = $"{u}@hotelos.local",
                    FullName = full,
                    PasswordHash = hasher.Hash(pwd),
                    RoleId = roles[role],
                    IsActive = true
                });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedRoomsAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Rooms.AnyAsync(ct)) return;

        var types = await db.RoomTypes.ToDictionaryAsync(t => t.Name, t => t.Id, ct);
        var now = DateTime.UtcNow;

        // number, floor, type, nearElevator, cleanedMinutesAgo
        var rooms = new (int N, int F, string T, bool E, int Mins)[]
        {
            (101, 1, RoomTypeNames.Single,     true,  200),
            (102, 1, RoomTypeNames.Double,     true,  60),
            (103, 1, RoomTypeNames.Double,     false, 300),
            (104, 1, RoomTypeNames.Suite,      false, 120),
            (105, 1, RoomTypeNames.Accessible, false, 90),
            (201, 2, RoomTypeNames.Single,     true,  30),
            (202, 2, RoomTypeNames.Deluxe,     true,  240),
            (203, 2, RoomTypeNames.Double,     false, 15),
            (204, 2, RoomTypeNames.Suite,      false, 180),
            (205, 2, RoomTypeNames.Accessible, false, 45)
        };

        foreach (var (n, f, t, e, mins) in rooms)
            db.Rooms.Add(new Room
            {
                Number = n,
                Floor = f,
                RoomTypeId = types[t],
                NearElevator = e,
                Status = RoomStatus.Clean,
                LastCleanedAt = now.AddMinutes(-mins)
            });

        await db.SaveChangesAsync(ct);
    }
}
