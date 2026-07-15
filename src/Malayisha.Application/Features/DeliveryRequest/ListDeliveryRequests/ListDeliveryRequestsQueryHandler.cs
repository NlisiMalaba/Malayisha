using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.DeliveryRequest.ListDeliveryRequests;

internal sealed class ListDeliveryRequestsQueryHandler(
    IDeliveryRequestRepository deliveryRequestRepository,
    ILogger<ListDeliveryRequestsQueryHandler> logger)
    : IRequestHandler<ListDeliveryRequestsQuery, Result<DeliveryRequestPageResponse>>
{
    public async Task<Result<DeliveryRequestPageResponse>> Handle(
        ListDeliveryRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var page = await deliveryRequestRepository.ListActiveAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = DeliveryRequestMappings.ToPage(page, request.Page, request.PageSize);

        logger.LogInformation(
            "Listed {Count} of {TotalCount} active delivery requests (page {Page})",
            response.Items.Count,
            response.TotalCount,
            response.Page);

        return Result<DeliveryRequestPageResponse>.Success(response);
    }
}
