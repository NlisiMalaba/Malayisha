using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.DeliveryRequest.UpdateDeliveryRequest;

public sealed record UpdateDeliveryRequestCommand(
    Guid UserId,
    Guid DeliveryRequestId,
    string OriginCity,
    string DestinationCity,
    DateTime RequiredDateUtc,
    decimal WeightKg,
    string SizeDescription,
    string GoodsDescription) : IRequest<Result<DeliveryRequestResponse>>;
