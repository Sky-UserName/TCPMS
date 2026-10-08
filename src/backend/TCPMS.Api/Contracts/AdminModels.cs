namespace TCPMS.Api.Contracts;

public sealed record RoomTypeRequest(
    Guid StoreId,
    string Name,
    string Kind,
    int BedCount,
    string? Gender,
    int MaxGuests,
    int BasePriceCents,
    string? Description,
    string? FacilitiesJson,
    string? ImageUrlsJson,
    bool IsPublished,
    string? ApprovalStatus,
    string? LongName = null,
    string? RoomCategory = null,
    string? TagsJson = null,
    string? ServicesJson = null,
    string? BathroomFacilitiesJson = null,
    string? NearbyFacilitiesJson = null,
    string? CheckInTime = null,
    string? CheckOutTime = null,
    string? CancellationRule = null,
    string? JoinRule = null,
    decimal? Area = null,
    string? Orientation = null,
    int RoomCount = 1,
    string? Layout = null,
    int DefaultInventory = 0,
    int SortOrder = 0,
    bool IsDraft = false,
    string? RejectionReason = null,
    int ChildCapacity = 0);

public sealed record RoomTypeReviewRequest(
    bool Approved,
    string? RejectionReason);

public sealed record RoomUnitRequest(
    Guid RoomTypeId,
    string Code,
    string Kind,
    string? Gender,
    string Status,
    string? Note);

public sealed record PriceCalendarItemRequest(
    Guid RoomTypeId,
    DateTime Date,
    int PriceCents,
    string? Source);

public sealed record BulkPriceRequest(
    Guid RoomTypeId,
    DateTime From,
    DateTime To,
    int PriceCents,
    string? Source);

public sealed record InventoryAdjustRequest(
    Guid RoomTypeId,
    DateTime From,
    DateTime To,
    int Delta,
    string Reason);

public sealed record CheckInRequest(
    IReadOnlyList<string>? ResourceCodes,
    string? Note);

public sealed record CreateRefundRequest(
    int AmountCents,
    string Reason);

public sealed record AdminUserRequest(
    string Username,
    string DisplayName,
    string? Password,
    string? PhoneNumber,
    Guid? StoreId,
    bool IsEnabled,
    IReadOnlyList<Guid>? RoleIds);

public sealed record AdminUserItem(
    Guid Id,
    string Username,
    string DisplayName,
    string? PhoneNumber,
    Guid? StoreId,
    string? StoreName,
    bool IsEnabled,
    DateTime? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record AdminMemberItem(
    Guid Id,
    string OpenId,
    string? Nickname,
    string? Phone,
    string? Avatar,
    string? Gender,
    int BalanceCents,
    int CommissionCents,
    int DirectUserCount,
    int IndirectUserCount,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    bool IsEnabled)
{
    // Keep screenshot-oriented aliases while retaining the API-wide cents convention.
    public int Balance => BalanceCents;
    public int Commission => CommissionCents;
}

public sealed record AdminMemberPage(
    IReadOnlyList<AdminMemberItem> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AdminMemberRequest(
    string OpenId,
    string? Nickname,
    string? PhoneNumber,
    string? AvatarUrl,
    string? Gender,
    int BalanceCents,
    int CommissionCents,
    Guid? ParentUserId,
    bool IsEnabled);

public sealed record RoleItem(
    Guid Id,
    string Name,
    string Code,
    int UserCount);

public sealed record RoomUnitItem(
    Guid Id,
    Guid StoreId,
    Guid RoomTypeId,
    string Code,
    string Kind,
    string? Gender,
    string Status,
    string? Note);

public sealed record AdminRoomTypeItem(
    Guid Id,
    Guid StoreId,
    string StoreName,
    string Name,
    string Kind,
    int BedCount,
    string? Gender,
    int MaxGuests,
    int BasePriceCents,
    string? Description,
    string? FacilitiesJson,
    string? ImageUrlsJson,
    bool IsPublished,
    string ApprovalStatus,
    int ResourceCount,
    string? LongName = null,
    string? RoomCategory = null,
    string? TagsJson = null,
    string? ServicesJson = null,
    string? BathroomFacilitiesJson = null,
    string? NearbyFacilitiesJson = null,
    string? CheckInTime = null,
    string? CheckOutTime = null,
    string? CancellationRule = null,
    string? JoinRule = null,
    decimal? Area = null,
    string? Orientation = null,
    int RoomCount = 1,
    string? Layout = null,
    int DefaultInventory = 0,
    int SortOrder = 0,
    bool IsDraft = false,
    string? RejectionReason = null,
    DateTime? ReviewedAt = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null,
    int TotalBookings = 0,
    int ChildCapacity = 0);

public sealed record PriceCalendarItem(
    Guid Id,
    Guid RoomTypeId,
    DateTime Date,
    int PriceCents,
    string Source);

public sealed record InventorySummaryItem(
    Guid Id,
    Guid StoreId,
    Guid RoomTypeId,
    DateTime Date,
    int TotalQuantity,
    int LockedQuantity,
    int SoldQuantity,
    int AvailableQuantity,
    DateTime UpdatedAt);

public sealed record AdminOrderDetail(
    Guid Id,
    string OrderNumber,
    Guid WxUserId,
    Guid StoreId,
    string StoreName,
    Guid RoomTypeId,
    string RoomTypeName,
    DateTime CheckIn,
    DateTime CheckOut,
    int Quantity,
    int GuestCount,
    int TotalAmountCents,
    string Currency,
    string Status,
    string PaymentStatus,
    string? GuestSnapshotJson,
    string? AssignedResourcesJson,
    DateTime? CheckedInAt,
    DateTime? CheckedOutAt,
    DateTime CreatedAt,
    DateTime? PaymentExpiresAt,
    IReadOnlyList<PaymentItem> Payments,
    IReadOnlyList<RefundItem> Refunds);

public sealed record PaymentItem(
    Guid Id,
    string TransactionNumber,
    string Provider,
    int AmountCents,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt);

public sealed record RefundItem(
    Guid Id,
    string RefundNumber,
    int AmountCents,
    string Reason,
    string Status,
    string? ReviewComment,
    string? ProviderRefundNumber,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    DateTime? CompletedAt);

public sealed record AdminRefundItem(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string StoreName,
    string RoomTypeName,
    int AmountCents,
    string Reason,
    string Status,
    string? ReviewComment,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    string? ProviderRefundNumber);

public sealed record DashboardTrendItem(
    DateTime Date,
    int OrderCount,
    int PaidOrderCount,
    int SalesCents,
    int RefundCents);

public sealed record DashboardStoreRankItem(
    Guid StoreId,
    string StoreName,
    int OrderCount,
    int SalesCents);
