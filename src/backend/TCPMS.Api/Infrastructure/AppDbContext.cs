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
    public DbSet<Region> Regions => Set<Region>();
    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<PriceCalendar> PriceCalendars => Set<PriceCalendar>();
    public DbSet<InventoryDaily> InventoryDailies => Set<InventoryDaily>();
    public DbSet<RoomUnit> RoomUnits => Set<RoomUnit>();
    public DbSet<BookingOrder> BookingOrders => Set<BookingOrder>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<RefundRecord> RefundRecords => Set<RefundRecord>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewModeration> ReviewModerations => Set<ReviewModeration>();
    public DbSet<AdminManagedItem> AdminManagedItems => Set<AdminManagedItem>();
    public DbSet<PermissionMenu> PermissionMenus => Set<PermissionMenu>();
    public DbSet<MiniProgramSubscriptionTemplate> SubscriptionTemplates => Set<MiniProgramSubscriptionTemplate>();
    public DbSet<MiniProgramMessageLog> MessageLogs => Set<MiniProgramMessageLog>();
    public DbSet<StoreStaffProfile> StoreStaffProfiles => Set<StoreStaffProfile>();
    public DbSet<WithdrawalRecord> WithdrawalRecords => Set<WithdrawalRecord>();
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

        modelBuilder.Entity<StoreStaffProfile>()
            .HasKey(x => x.AdminUserId);

        modelBuilder.Entity<StoreStaffProfile>()
            .Property(x => x.Gender)
            .HasMaxLength(20);

        modelBuilder.Entity<StoreStaffProfile>()
            .Property(x => x.PermissionKeysJson)
            .HasColumnType("longtext");

        modelBuilder.Entity<StoreStaffProfile>()
            .HasOne(x => x.AdminUser)
            .WithOne(x => x.StoreStaffProfile)
            .HasForeignKey<StoreStaffProfile>(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Store>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Store>()
            .HasIndex(x => new { x.Status, x.SortOrder });

        modelBuilder.Entity<Region>()
            .HasKey(x => x.Code);

        modelBuilder.Entity<Region>()
            .Property(x => x.Code)
            .HasMaxLength(12)
            .IsUnicode(false);

        modelBuilder.Entity<Region>()
            .Property(x => x.ParentCode)
            .HasMaxLength(12)
            .IsUnicode(false);

        modelBuilder.Entity<Region>()
            .Property(x => x.Level)
            .HasMaxLength(32)
            .IsUnicode(false);

        modelBuilder.Entity<Region>()
            .HasIndex(x => new { x.ParentCode, x.SortOrder });

        modelBuilder.Entity<Region>()
            .HasIndex(x => new { x.Level, x.IsEnabled, x.SortOrder });

        modelBuilder.Entity<Region>()
            .HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCode)
            .HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);

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

        modelBuilder.Entity<ReviewModeration>()
            .HasKey(x => x.ReviewId);

        modelBuilder.Entity<ReviewModeration>()
            .Property(x => x.Status)
            .HasMaxLength(32)
            .IsUnicode(false);

        modelBuilder.Entity<ReviewModeration>()
            .Property(x => x.ReviewComment)
            .HasMaxLength(500);

        modelBuilder.Entity<ReviewModeration>()
            .HasOne(x => x.Review)
            .WithOne(x => x.Moderation)
            .HasForeignKey<ReviewModeration>(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReviewModeration>()
            .HasIndex(x => new { x.Status, x.UpdatedAt });

        modelBuilder.Entity<AdminManagedItem>()
            .Property(x => x.ResourceType)
            .HasMaxLength(80)
            .IsUnicode(false);

        modelBuilder.Entity<AdminManagedItem>()
            .Property(x => x.DataJson)
            .HasColumnType("longtext");

        modelBuilder.Entity<AdminManagedItem>()
            .HasIndex(x => new { x.ResourceType, x.UpdatedAt });

        modelBuilder.Entity<PermissionMenu>()
            .HasKey(x => x.Key);

        modelBuilder.Entity<PermissionMenu>()
            .Property(x => x.Key)
            .HasMaxLength(80)
            .IsUnicode(false);

        modelBuilder.Entity<PermissionMenu>()
            .Property(x => x.Title)
            .HasMaxLength(120);

        modelBuilder.Entity<PermissionMenu>()
            .Property(x => x.IconKey)
            .HasMaxLength(80)
            .IsUnicode(false);

        modelBuilder.Entity<PermissionMenu>()
            .Property(x => x.IconUrl)
            .HasColumnType("longtext");

        modelBuilder.Entity<PermissionMenu>()
            .Property(x => x.Route)
            .HasMaxLength(200)
            .IsUnicode(false);

        modelBuilder.Entity<PermissionMenu>()
            .Property(x => x.Status)
            .HasMaxLength(20)
            .IsUnicode(false);

        modelBuilder.Entity<PermissionMenu>()
            .HasIndex(x => new { x.Status, x.Weight });

        modelBuilder.Entity<MiniProgramSubscriptionTemplate>()
            .Property(x => x.Identifier)
            .HasMaxLength(100)
            .IsUnicode(false);

        modelBuilder.Entity<MiniProgramSubscriptionTemplate>()
            .Property(x => x.ParametersJson)
            .HasColumnType("longtext");

        modelBuilder.Entity<MiniProgramSubscriptionTemplate>()
            .HasIndex(x => x.Identifier)
            .IsUnique();

        modelBuilder.Entity<MiniProgramSubscriptionTemplate>()
            .HasIndex(x => new { x.IsEnabled, x.UpdatedAt });

        modelBuilder.Entity<MiniProgramMessageLog>()
            .Property(x => x.TemplateDataJson)
            .HasColumnType("longtext");

        modelBuilder.Entity<MiniProgramMessageLog>()
            .Property(x => x.SendResultJson)
            .HasColumnType("longtext");

        modelBuilder.Entity<MiniProgramMessageLog>()
            .HasIndex(x => new { x.CreatedAt, x.SendStatus });

        modelBuilder.Entity<MiniProgramMessageLog>()
            .HasIndex(x => x.OpenId);

        modelBuilder.Entity<MiniProgramMessageLog>()
            .HasIndex(x => x.MemberId);

        modelBuilder.Entity<MiniProgramMessageLog>()
            .HasOne(x => x.SubscriptionTemplate)
            .WithMany(x => x.MessageLogs)
            .HasForeignKey(x => x.SubscriptionTemplateId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MiniProgramMessageLog>()
            .HasOne(x => x.Member)
            .WithMany()
            .HasForeignKey(x => x.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WithdrawalRecord>()
            .Property(x => x.Account)
            .HasMaxLength(200);

        modelBuilder.Entity<WithdrawalRecord>()
            .Property(x => x.Status)
            .HasMaxLength(32)
            .IsUnicode(false);

        modelBuilder.Entity<WithdrawalRecord>()
            .Property(x => x.ReviewComment)
            .HasMaxLength(500);

        modelBuilder.Entity<WithdrawalRecord>()
            .HasIndex(x => new { x.StoreId, x.Status, x.CreatedAt });

        modelBuilder.Entity<WithdrawalRecord>()
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

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
