using Malayisha.Domain.Common;
using MediatR;

namespace Malayisha.Application.Features.DeliveryRequest.CancelDeliveryRequest;

public sealed record CancelDeliveryRequestCommand(
    Guid UserId,
    Guid DeliveryRequestId) : IRequest<Result>;
