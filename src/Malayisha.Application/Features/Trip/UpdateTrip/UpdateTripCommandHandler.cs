using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Trip.UpdateTrip;

internal sealed class UpdateTripCommandHandler(
    ITransporterProfileRepository profileRepository,
    ITripListingRepository tripListingRepository,
    TimeProvider timeProvider,
    ILogger<UpdateTripCommandHandler> logger)
    : IRequestHandler<UpdateTripCommand, Result<TripListingResponse>>
{
    public async Task<Result<TripListingResponse>> Handle(
        UpdateTripCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result<TripListingResponse>.Error(TripErrorCodes.ProfileNotFound);
        }

        var trip = await tripListingRepository.FindByIdAsync(request.TripListingId, cancellationToken);
        if (trip is null || trip.IsDeleted)
        {
            return Result<TripListingResponse>.Error(TripErrorCodes.TripNotFound);
        }

        if (trip.TransporterProfileId != profile.Id)
        {
            return Result<TripListingResponse>.Error(TripErrorCodes.NotTripOwner);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!TripDateRules.IsFutureDepartureDate(request.DepartureDateUtc, nowUtc))
        {
            return Result<TripListingResponse>.Error(TripErrorCodes.DepartureDateMustBeFuture);
        }

        trip.Update(
            request.OriginCity,
            request.DestinationCity,
            request.DepartureDateUtc,
            request.AvailableCapacityKg,
            request.PriceGuideZar,
            request.Description,
            nowUtc);

        await tripListingRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated trip listing {TripListingId} for profile {ProfileId}",
            trip.Id,
            profile.Id);

        return Result<TripListingResponse>.Success(TripMappings.ToResponse(trip));
    }
}
