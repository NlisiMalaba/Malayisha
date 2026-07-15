using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.DeliveryRequest.CreateDeliveryRequest;

public sealed record CreateDeliveryRequestCommand(
    Guid UserId,
    string OriginCity,
    string DestinationCity,
    DateTime RequiredDateUtc,
    decimal WeightKg,
    string SizeDescription,
    string GoodsDescription) : IRequest<Result<DeliveryRequestResponse>>;
