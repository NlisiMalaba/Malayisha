using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.DeliveryRequest.CancelDeliveryRequest;

internal sealed class CancelDeliveryRequestCommandHandler(
    IDeliveryRequestRepository deliveryRequestRepository,
    TimeProvider timeProvider,
    ILogger<CancelDeliveryRequestCommandHandler> logger)
    : IRequestHandler<CancelDeliveryRequestCommand, Result>
{
    public async Task<Result> Handle(
        CancelDeliveryRequestCommand request,
        CancellationToken cancellationToken)
    {
        var deliveryRequest = await deliveryRequestRepository.FindByIdAsync(
            request.DeliveryRequestId,
            cancellationToken);

        if (deliveryRequest is null || deliveryRequest.Status != DeliveryRequestStatus.Active)
        {
            return Result.Error(DeliveryRequestErrorCodes.DeliveryRequestNotFound);
        }

        if (deliveryRequest.SenderId != request.UserId)
        {
            return Result.Error(DeliveryRequestErrorCodes.NotDeliveryRequestOwner);
        }

        if (await deliveryRequestRepository.HasBlockingBookingsAsync(deliveryRequest.Id, cancellationToken))
        {
            return Result.Error(DeliveryRequestErrorCodes.ActiveBookingsBlockCancel);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        deliveryRequest.MarkCancelled(nowUtc);

        await deliveryRequestRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Cancelled delivery request {DeliveryRequestId} for sender {SenderId}",
            deliveryRequest.Id,
            deliveryRequest.SenderId);

        return Result.Success();
    }
}
