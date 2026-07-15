using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.DeliveryRequest.UpdateDeliveryRequest;

internal sealed class UpdateDeliveryRequestCommandHandler(
    IDeliveryRequestRepository deliveryRequestRepository,
    TimeProvider timeProvider,
    ILogger<UpdateDeliveryRequestCommandHandler> logger)
    : IRequestHandler<UpdateDeliveryRequestCommand, Result<DeliveryRequestResponse>>
{
    public async Task<Result<DeliveryRequestResponse>> Handle(
        UpdateDeliveryRequestCommand request,
        CancellationToken cancellationToken)
    {
        var deliveryRequest = await deliveryRequestRepository.FindByIdAsync(
            request.DeliveryRequestId,
            cancellationToken);

        if (deliveryRequest is null || deliveryRequest.Status != DeliveryRequestStatus.Active)
        {
            return Result<DeliveryRequestResponse>.Error(DeliveryRequestErrorCodes.DeliveryRequestNotFound);
        }

        if (deliveryRequest.SenderId != request.UserId)
        {
            return Result<DeliveryRequestResponse>.Error(DeliveryRequestErrorCodes.NotDeliveryRequestOwner);
        }

        if (await deliveryRequestRepository.HasAssociatedBookingAsync(deliveryRequest.Id, cancellationToken))
        {
            return Result<DeliveryRequestResponse>.Error(DeliveryRequestErrorCodes.AssociatedBookingBlocksUpdate);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!DeliveryRequestDateRules.IsFutureRequiredDate(request.RequiredDateUtc, nowUtc))
        {
            return Result<DeliveryRequestResponse>.Error(DeliveryRequestErrorCodes.RequiredDateMustBeFuture);
        }

        deliveryRequest.Update(
            request.OriginCity,
            request.DestinationCity,
            request.RequiredDateUtc,
            request.WeightKg,
            request.SizeDescription,
            request.GoodsDescription,
            nowUtc);

        await deliveryRequestRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated delivery request {DeliveryRequestId} for sender {SenderId}",
            deliveryRequest.Id,
            deliveryRequest.SenderId);

        return Result<DeliveryRequestResponse>.Success(DeliveryRequestMappings.ToResponse(deliveryRequest));
    }
}
