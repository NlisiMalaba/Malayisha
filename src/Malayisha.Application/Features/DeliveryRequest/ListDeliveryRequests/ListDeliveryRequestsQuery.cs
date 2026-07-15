using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.DeliveryRequest.ListDeliveryRequests;

public sealed record ListDeliveryRequestsQuery(
    int Page = DeliveryRequestValidation.DefaultPage,
    int PageSize = DeliveryRequestValidation.DefaultPageSize) : IRequest<Result<DeliveryRequestPageResponse>>;
