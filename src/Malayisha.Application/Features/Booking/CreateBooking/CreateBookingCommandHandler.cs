using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Booking;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Booking.CreateBooking;

internal sealed class CreateBookingCommandHandler(
    IBookingRepository bookingRepository,
    ITripListingRepository tripListingRepository,
    ITransporterProfileRepository transporterProfileRepository,
    IDeliveryRequestRepository deliveryRequestRepository,
    TimeProvider timeProvider,
    ILogger<CreateBookingCommandHandler> logger) : IRequestHandler<CreateBookingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var tripListing = await tripListingRepository.FindByIdAsync(request.TripListingId, cancellationToken);
        if (tripListing is null || tripListing.IsDeleted)
        {
            return Result<Guid>.Error(BookingErrorCodes.TripNotFound);
        }

        var transporterProfile = await transporterProfileRepository.FindByIdAsync(
            tripListing.TransporterProfileId,
            cancellationToken);

        if (transporterProfile is null)
        {
            return Result<Guid>.Error(BookingErrorCodes.TransporterProfileNotFound);
        }

        if (transporterProfile.UserId == request.SenderId)
        {
            return Result<Guid>.Error(BookingErrorCodes.SelfBookingNotAllowed);
        }

        Malayisha.Domain.Entities.DeliveryRequest? deliveryRequest = null;
        if (request.DeliveryRequestId.HasValue)
        {
            deliveryRequest = await deliveryRequestRepository.FindByIdAsync(
                request.DeliveryRequestId.Value,
                cancellationToken);

            if (deliveryRequest is null)
            {
                return Result<Guid>.Error(BookingErrorCodes.DeliveryRequestNotFound);
            }

            if (deliveryRequest.Status != DeliveryRequestStatus.Active)
            {
                return Result<Guid>.Error(BookingErrorCodes.DeliveryRequestNotActive);
            }

            if (deliveryRequest.SenderId != request.SenderId)
            {
                return Result<Guid>.Error(BookingErrorCodes.DeliveryRequestNotOwnedBySender);
            }

            if (await deliveryRequestRepository.HasAssociatedBookingAsync(deliveryRequest.Id, cancellationToken))
            {
                return Result<Guid>.Error(BookingErrorCodes.DeliveryRequestAlreadyBooked);
            }
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var booking = Malayisha.Domain.Entities.Booking.Create(
            Guid.NewGuid(),
            tripListing.Id,
            request.SenderId,
            transporterProfile.UserId,
            nowUtc,
            request.Message,
            request.DeliveryRequestId);

        await bookingRepository.AddAsync(booking, cancellationToken);

        if (deliveryRequest is not null)
        {
            deliveryRequest.MarkConvertedToBooking(nowUtc);
        }

        await bookingRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created booking {BookingId} for sender {SenderId} and transporter {TransporterId}",
            booking.Id,
            booking.SenderId,
            booking.TransporterId);

        return Result<Guid>.Success(booking.Id);
    }
}
