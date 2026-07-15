using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.DeliveryRequest.CreateDeliveryRequest;

internal sealed class CreateDeliveryRequestCommandHandler(
    IDeliveryRequestRepository deliveryRequestRepository,
    TimeProvider timeProvider,
    ILogger<CreateDeliveryRequestCommandHandler> logger)
    : IRequestHandler<CreateDeliveryRequestCommand, Result<DeliveryRequestResponse>>
{
    public async Task<Result<DeliveryRequestResponse>> Handle(
        CreateDeliveryRequestCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!DeliveryRequestDateRules.IsFutureRequiredDate(request.RequiredDateUtc, nowUtc))
        {
            return Result<DeliveryRequestResponse>.Error(DeliveryRequestErrorCodes.RequiredDateMustBeFuture);
        }

        var deliveryRequest = Domain.Entities.DeliveryRequest.Create(
            Guid.NewGuid(),
            request.UserId,
            request.OriginCity,
            request.DestinationCity,
            request.RequiredDateUtc,
            request.WeightKg,
            request.SizeDescription,
            request.GoodsDescription,
            nowUtc);

        await deliveryRequestRepository.AddAsync(deliveryRequest, cancellationToken);
        await deliveryRequestRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created delivery request {DeliveryRequestId} for sender {SenderId}",
            deliveryRequest.Id,
            deliveryRequest.SenderId);

        return Result<DeliveryRequestResponse>.Success(DeliveryRequestMappings.ToResponse(deliveryRequest));
    }
}
