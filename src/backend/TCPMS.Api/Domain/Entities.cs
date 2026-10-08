namespace TCPMS.Api.Domain;

public static class SystemRoles
{
    public const string Admin = "总部管理员";
    public const string Operations = "运营审核";
    public const string Finance = "财务";
    public const string StoreAdmin = "门店管理员";
    public const string StoreStaff = "门店员工";
}

public static class RoleCodes
{
    public const string SystemAdmin = "system_admin";
    public const string Operations = "operations";
    public const string Finance = "finance";
    public const string StoreAdmin = "store_admin";
    public const string StoreStaff = "store_staff";
}

public static class StoreStatuses
{
    public const string Draft = "Draft";
    public const string PendingReview = "PendingReview";
    public const string Active = "Active";
    public const string Offline = "Offline";
    public const string Disabled = "Disabled";
}

public static class OrderStatuses
{
    public const string PendingPayment = "PendingPayment";
    public const string PaidPendingConfirmation = "PaidPendingConfirmation";
    public const string ConfirmedPendingCheckIn = "ConfirmedPendingCheckIn";
    public const string CheckedIn = "CheckedIn";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Refunding = "Refunding";
    public const string Refunded = "Refunded";
    public const string RefundFailed = "RefundFailed";
}

public static class RoomTypeKinds
{
    public const string PrivateRoom = "PrivateRoom";
    public const string Bed = "Bed";
}

public class WxUser
{
    public Guid Id { get; set; }
    public string OpenId { get; set; } = string.Empty;
    public string? UnionId { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public int BalanceCents { get; set; }
    public int CommissionCents { get; set; }
    public Guid? ParentUserId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public WxUser? ParentUser { get; set; }
    public ICollection<WxUser> Referrals { get; set; } = new List<WxUser>();
}

public class AdminUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Guid? StoreId { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Store? Store { get; set; }
    public ICollection<AdminUserRole> UserRoles { get; set; } = new List<AdminUserRole>();
}

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ICollection<AdminUserRole> UserRoles { get; set; } = new List<AdminUserRole>();
}

public class AdminUserRole
{
    public Guid AdminUserId { get; set; }
    public Guid RoleId { get; set; }
    public AdminUser? AdminUser { get; set; }
    public Role? Role { get; set; }
}

public class Store
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = StoreStatuses.Draft;
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
    public string? StandardAddress { get; set; }
    public string? AdministrativeAreaCode { get; set; }
    public string? Phone { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? Latitude { get; set; }
    public string? AmapPoiId { get; set; }
    public string? CoordinateSource { get; set; }
    public DateTime? CoordinatesUpdatedAt { get; set; }
    public string? OwnerUsername { get; set; }
    public string? OwnerNickname { get; set; }
    public string? OwnerAvatarUrl { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? IdCardFrontUrl { get; set; }
    public string? IdCardBackUrl { get; set; }
    public string? PropertyCertificateUrl { get; set; }
    public string? BusinessLicenseUrl { get; set; }
    public string? DoorImageUrl { get; set; }
    public string? RoomImageUrlsJson { get; set; }
    public decimal? Rating { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();
}

public class RoomType
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LongName { get; set; }
    public string Kind { get; set; } = RoomTypeKinds.PrivateRoom;
    public string? RoomCategory { get; set; }
    public int BedCount { get; set; } = 1;
    public string? Gender { get; set; }
    public int MaxGuests { get; set; } = 1;
    public int ChildCapacity { get; set; }
    public int BasePriceCents { get; set; }
    public string? Description { get; set; }
    public string? FacilitiesJson { get; set; }
    public string? TagsJson { get; set; }
    public string? ServicesJson { get; set; }
    public string? BathroomFacilitiesJson { get; set; }
    public string? NearbyFacilitiesJson { get; set; }
    public string? ImageUrlsJson { get; set; }
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }
    public string? CancellationRule { get; set; }
    public string? JoinRule { get; set; }
    public decimal? Area { get; set; }
    public string? Orientation { get; set; }
    public int RoomCount { get; set; } = 1;
    public string? Layout { get; set; }
    public int DefaultInventory { get; set; }
    public int SortOrder { get; set; }
    public bool IsDraft { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public bool IsPublished { get; set; } = true;
    public string ApprovalStatus { get; set; } = "Approved";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Store? Store { get; set; }
}

public class PriceCalendar
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public int PriceCents { get; set; }
    public string Source { get; set; } = "Default";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class InventoryDaily
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public int TotalQuantity { get; set; }
    public int LockedQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int AvailableQuantity => Math.Max(0, TotalQuantity - LockedQuantity - SoldQuantity);
}

public class RoomUnit
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public Guid RoomTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Kind { get; set; } = "Room";
    public string? Gender { get; set; }
    public string Status { get; set; } = "Available";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Store? Store { get; set; }
    public RoomType? RoomType { get; set; }
}

public class BookingOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid WxUserId { get; set; }
    public Guid StoreId { get; set; }
    public Guid RoomTypeId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int Quantity { get; set; }
    public int GuestCount { get; set; }
    public int TotalAmountCents { get; set; }
    public string Currency { get; set; } = "CNY";
    public string Status { get; set; } = OrderStatuses.PendingPayment;
    public string PaymentStatus { get; set; } = "Unpaid";
    public string? CancellationReason { get; set; }
    public string? GuestSnapshotJson { get; set; }
    public string? PriceSnapshotJson { get; set; }
    public string? AssignedResourcesJson { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime PaymentExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Store? Store { get; set; }
    public RoomType? RoomType { get; set; }
}

public class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string Provider { get; set; } = "WeChat";
    public int AmountCents { get; set; }
    public string Status { get; set; } = "Pending";
    public string? RawCallback { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public BookingOrder? Order { get; set; }
}

public class RefundRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string RefundNumber { get; set; } = string.Empty;
    public int AmountCents { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingReview";
    public string? OriginalOrderStatus { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public Guid? ReviewedByAdminId { get; set; }
    public string? ReviewComment { get; set; }
    public string? ProviderRefundNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public BookingOrder? Order { get; set; }
}

public class Review
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid WxUserId { get; set; }
    public Guid StoreId { get; set; }
    public Guid RoomTypeId { get; set; }
    public int RoomRating { get; set; }
    public int StoreRating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrlsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BookingOrder? Order { get; set; }
    public WxUser? User { get; set; }
    public Store? Store { get; set; }
    public RoomType? RoomType { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorType { get; set; } = "System";
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public string? IpAddress { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
