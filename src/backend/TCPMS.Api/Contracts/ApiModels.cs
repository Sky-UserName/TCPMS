namespace TCPMS.Api.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record MiniappLoginRequest(
    string? Code,
    string? DevOpenId,
    string? Nickname,
    string? AvatarUrl);

public sealed record StoreRequest(
    string Name,
    string Code,
    string? Description,
    string? Province,
    string? City,
    string? District,
    string? Address,
    string? Phone,
    decimal? Longitude,
    decimal? Latitude,
    string? Status,
    int SortOrder,
    string? OwnerUsername = null,
    string? OwnerNickname = null,
    string? OwnerAvatarUrl = null,
    string? OwnerName = null,
    string? OwnerPhone = null,
    string? IdCardFrontUrl = null,
    string? IdCardBackUrl = null,
    string? PropertyCertificateUrl = null,
    string? BusinessLicenseUrl = null,
    string? DoorImageUrl = null,
    string? RoomImageUrlsJson = null,
    decimal? Rating = null);

public sealed record GeocodeRequest(string Address, string? City);

public sealed record ReverseGeocodeRequest(decimal Longitude, decimal Latitude);

public sealed record OrderQuoteRequest(
    Guid StoreId,
    Guid RoomTypeId,
    DateTime CheckIn,
    DateTime CheckOut,
    int Quantity,
    int GuestCount = 1);

public sealed record CreateOrderRequest(
    Guid StoreId,
    Guid RoomTypeId,
    DateTime CheckIn,
    DateTime CheckOut,
    int Quantity,
    int GuestCount,
    string? GuestSnapshotJson);

public sealed record CreateReviewRequest(
    int RoomRating,
    int StoreRating,
    string? Content,
    IReadOnlyList<string>? ImageUrls);

public sealed record ReviewRefundRequest(bool Approved, string? Comment);

public sealed record ManualInventoryRequest(
    Guid RoomTypeId,
    DateTime Date,
    int Delta,
    string Reason);

public sealed record ApiError(string Code, string Message, string? TraceId = null);

public sealed record HealthResponse(
    string Status,
    string Service,
    string Environment,
    string DatabaseProvider,
    DateTime Utc);

public sealed record MapConfigResponse(
    bool AmapEnabled,
    string? MiniProgramKey,
    double DefaultRadiusKm);

public sealed record AdminIdentityResponse(
    Guid? Id,
    string? Username,
    string? DisplayName,
    IReadOnlyList<string> Roles,
    Guid? StoreId);

public sealed record AdminStoreDetail(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Province,
    string? City,
    string? District,
    string? Address,
    string? StandardAddress,
    string? AdministrativeAreaCode,
    string? Phone,
    decimal? Longitude,
    decimal? Latitude,
    string? AmapPoiId,
    string? CoordinateSource,
    DateTime? CoordinatesUpdatedAt,
    bool HasMapLocation,
    string Status,
    int SortOrder,
    int RoomTypeCount,
    string? OwnerUsername = null,
    string? OwnerNickname = null,
    string? OwnerAvatarUrl = null,
    string? OwnerName = null,
    string? OwnerPhone = null,
    string? IdCardFrontUrl = null,
    string? IdCardBackUrl = null,
    string? PropertyCertificateUrl = null,
    string? BusinessLicenseUrl = null,
    string? DoorImageUrl = null,
    string? RoomImageUrlsJson = null,
    decimal? Rating = null);

public sealed record UpdatedCountResponse(int Updated);

public sealed record RefundCreatedResponse(
    Guid Id,
    string RefundNumber,
    int AmountCents,
    string Status,
    DateTime CreatedAt);

public sealed record TokenResponse(
    string Token,
    DateTime ExpiresAt,
    string UserType,
    string DisplayName);

public sealed record StoreListItem(
    Guid Id,
    string Name,
    string? Address,
    string? Phone,
    string Status,
    decimal? Longitude,
    decimal? Latitude,
    double? DistanceKm,
    bool HasMapLocation,
    string? OwnerName = null,
    string? OwnerPhone = null,
    string? OwnerAvatarUrl = null,
    decimal? Rating = null);

public sealed record RoomTypeSummary(
    Guid Id,
    Guid StoreId,
    string Name,
    string Kind,
    int BedCount,
    string? Gender,
    int MaxGuests,
    int BasePriceCents,
    string? Description,
    bool IsPublished);

public sealed record StoreDetail(
    StoreListItem Store,
    IReadOnlyList<RoomTypeSummary> RoomTypes);

public sealed record AvailabilityItem(
    DateTime Date,
    int TotalQuantity,
    int LockedQuantity,
    int SoldQuantity,
    int AvailableQuantity,
    int PriceCents);

public sealed record OrderQuote(
    Guid StoreId,
    Guid RoomTypeId,
    DateTime CheckIn,
    DateTime CheckOut,
    int Nights,
    int Quantity,
    int GuestCount,
    int TotalAmountCents,
    IReadOnlyList<AvailabilityItem> DailyItems);

public sealed record OrderListItem(
    Guid Id,
    string OrderNumber,
    string StoreName,
    string RoomTypeName,
    DateTime CheckIn,
    DateTime CheckOut,
    int Quantity,
    int TotalAmountCents,
    string Status,
    string PaymentStatus,
    DateTime CreatedAt,
    Guid StoreId,
    Guid RoomTypeId,
    bool HasReview);

public sealed record ReviewItem(
    Guid Id,
    Guid OrderId,
    string Nickname,
    string? AvatarUrl,
    DateTime CreatedAt,
    DateTime? CheckIn,
    int RoomRating,
    int StoreRating,
    string Content,
    IReadOnlyList<string> ImageUrls);

public sealed record RoomReviewResponse(
    Guid RoomTypeId,
    Guid StoreId,
    string RoomTypeName,
    string StoreName,
    decimal RoomAverageRating,
    decimal StoreAverageRating,
    int ReviewCount,
    IReadOnlyList<ReviewItem> Items);

public sealed record AdminMetrics(
    int StoreCount,
    int ActiveStoreCount,
    int PendingOrderCount,
    int TodayOrderCount,
    int TodaySalesCents,
    int PendingRefundCount);

public sealed record GeoResult(
    decimal Longitude,
    decimal Latitude,
    string? FormattedAddress,
    string? Province,
    string? City,
    string? District,
    string? AdministrativeAreaCode,
    string? PoiId);
