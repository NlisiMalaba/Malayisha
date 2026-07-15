using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.DeliveryRequest;

public sealed record DeliveryRequestResponse(
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

public sealed record DeliveryRequestPageResponse(
    IReadOnlyList<DeliveryRequestResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
