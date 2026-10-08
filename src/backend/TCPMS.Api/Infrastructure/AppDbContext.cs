using Microsoft.EntityFrameworkCore;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<WxUser> WxUsers => Set<WxUser>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<PriceCalendar> PriceCalendars => Set<PriceCalendar>();
    public DbSet<InventoryDaily> InventoryDailies => Set<InventoryDaily>();
    public DbSet<RoomUnit> RoomUnits => Set<RoomUnit>();
    public DbSet<BookingOrder> BookingOrders => Set<BookingOrder>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<RefundRecord> RefundRecords => Set<RefundRecord>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WxUser>()
            .HasIndex(x => x.OpenId)
            .IsUnique();

        modelBuilder.Entity<WxUser>()
            .HasIndex(x => x.ParentUserId);

        modelBuilder.Entity<WxUser>()
            .HasOne(x => x.ParentUser)
            .WithMany(x => x.Referrals)
            .HasForeignKey(x => x.ParentUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdminUser>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<AdminUserRole>()
            .HasKey(x => new { x.AdminUserId, x.RoleId });

        modelBuilder.Entity<AdminUser>()
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Store>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Store>()
            .HasIndex(x => new { x.Status, x.SortOrder });

        modelBuilder.Entity<Store>()
            .Property(x => x.Longitude)
            .HasPrecision(10, 6);

        modelBuilder.Entity<Store>()
            .Property(x => x.Latitude)
            .HasPrecision(10, 6);

        modelBuilder.Entity<RoomType>()
            .HasIndex(x => new { x.StoreId, x.Name })
            .IsUnique();

        modelBuilder.Entity<RoomUnit>()
            .HasIndex(x => new { x.RoomTypeId, x.Code })
            .IsUnique();

        modelBuilder.Entity<RoomUnit>()
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoomUnit>()
            .HasOne(x => x.RoomType)
            .WithMany()
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PriceCalendar>()
            .HasIndex(x => new { x.RoomTypeId, x.Date })
            .IsUnique();

        modelBuilder.Entity<InventoryDaily>()
            .HasIndex(x => new { x.RoomTypeId, x.Date })
            .IsUnique();

        modelBuilder.Entity<BookingOrder>()
            .HasIndex(x => x.OrderNumber)
            .IsUnique();

        modelBuilder.Entity<BookingOrder>()
            .HasIndex(x => new { x.WxUserId, x.CreatedAt });

        modelBuilder.Entity<PaymentRecord>()
            .HasIndex(x => x.TransactionNumber)
            .IsUnique();

        modelBuilder.Entity<PaymentRecord>()
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RefundRecord>()
            .HasIndex(x => x.RefundNumber)
            .IsUnique();

        modelBuilder.Entity<RefundRecord>()
            .HasIndex(x => new { x.OrderId, x.Status });

        modelBuilder.Entity<Review>()
            .HasIndex(x => x.OrderId)
            .IsUnique();

        modelBuilder.Entity<Review>()
            .HasIndex(x => new { x.RoomTypeId, x.CreatedAt });

        modelBuilder.Entity<Review>()
            .HasIndex(x => new { x.StoreId, x.CreatedAt });

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.WxUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Review>()
            .HasOne(x => x.RoomType)
            .WithMany()
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
