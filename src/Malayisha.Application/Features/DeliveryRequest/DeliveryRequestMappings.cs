using Malayisha.Application.Abstractions.Persistence;

namespace Malayisha.Application.Features.DeliveryRequest;

internal static class DeliveryRequestMappings
{
    public static DeliveryRequestResponse ToResponse(Domain.Entities.DeliveryRequest request) =>
        new(
            request.Id,
            request.SenderId,
            request.OriginCity,
            request.DestinationCity,
            request.RequiredDateUtc,
            request.WeightKg,
            request.SizeDescription,
            request.GoodsDescription,
            request.Status,
            request.CreatedAtUtc,
            request.UpdatedAtUtc);

    public static DeliveryRequestPageResponse ToPage(
        DeliveryRequestListPage page,
        int pageNumber,
        int pageSize) =>
        new(
            page.Items.Select(ToResponse).ToArray(),
            pageNumber,
            pageSize,
            page.TotalCount);
}
