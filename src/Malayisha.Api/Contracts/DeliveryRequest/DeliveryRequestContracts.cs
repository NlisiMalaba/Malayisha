using Malayisha.Domain.Enums;

namespace Malayisha.Api.Contracts.DeliveryRequest;

public sealed record CreateDeliveryRequestRequest(
    string OriginCity,
    string DestinationCity,
    DateTime RequiredDateUtc,
    decimal WeightKg,
    string SizeDescription,
    string GoodsDescription);

public sealed record UpdateDeliveryRequestRequest(
    string OriginCity,
    string DestinationCity,
    DateTime RequiredDateUtc,
    decimal WeightKg,
    string SizeDescription,
    string GoodsDescription);

public sealed record ListDeliveryRequestsRequest(
    int Page = 1,
    int PageSize = 20);

public sealed record DeliveryRequestDto(
    Guid Id,
    Guid SenderId,
    string OriginCity,
    string DestinationCity,
    DateTime RequiredDateUtc,
    decimal WeightKg,
    string SizeDescription,
    string GoodsDescription,
    DeliveryRequestStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record DeliveryRequestPageDto(
    IReadOnlyList<DeliveryRequestDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
