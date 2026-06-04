using HotelOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelOS.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("Reservations");
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferenceCode).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.ReferenceCode).IsUnique();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProximityPreference).HasMaxLength(20);
        b.Ignore(x => x.Nights);

        b.HasOne(x => x.Guest).WithMany(g => g.Reservations).HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RoomType).WithMany().HasForeignKey(x => x.RoomTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Room).WithMany(r => r.Reservations).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> b)
    {
        b.ToTable("Bills");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.Subtotal);
        b.Ignore(x => x.DiscountTotal);
        b.Ignore(x => x.Total);
        b.Ignore(x => x.Paid);
        b.Ignore(x => x.Balance);

        b.HasOne(x => x.Reservation).WithOne(r => r.Bill).HasForeignKey<Bill>(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Items).WithOne(i => i.Bill).HasForeignKey(i => i.BillId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Payments).WithOne(p => p.Bill).HasForeignKey(p => p.BillId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> b)
    {
        b.ToTable("BillItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Amount).HasPrecision(18, 2);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Reference).HasMaxLength(100);
    }
}

public class RoomServiceOrderConfiguration : IEntityTypeConfiguration<RoomServiceOrder>
{
    public void Configure(EntityTypeBuilder<RoomServiceOrder> b)
    {
        b.ToTable("RoomServiceOrders");
        b.HasKey(x => x.Id);
        b.Property(x => x.OrderNumber).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.Total);
        b.HasMany(x => x.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoomServiceOrderItemConfiguration : IEntityTypeConfiguration<RoomServiceOrderItem>
{
    public void Configure(EntityTypeBuilder<RoomServiceOrderItem> b)
    {
        b.ToTable("RoomServiceOrderItems");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Ignore(x => x.LineTotal);
    }
}

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> b)
    {
        b.ToTable("MaintenanceRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).IsRequired().HasMaxLength(300);
        b.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class HousekeepingTaskConfiguration : IEntityTypeConfiguration<HousekeepingTask>
{
    public void Configure(EntityTypeBuilder<HousekeepingTask> b)
    {
        b.ToTable("HousekeepingTasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Message).IsRequired().HasMaxLength(300);
        b.Property(x => x.TargetRole).HasMaxLength(50);
        b.HasIndex(x => x.UserId);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).IsRequired().HasMaxLength(80);
        b.Property(x => x.Username).HasMaxLength(50);
        b.Property(x => x.Entity).HasMaxLength(80);
        b.Property(x => x.EntityId).HasMaxLength(80);
        b.Property(x => x.Details).HasMaxLength(1000);
        b.Property(x => x.IpAddress).HasMaxLength(64);
    }
}
