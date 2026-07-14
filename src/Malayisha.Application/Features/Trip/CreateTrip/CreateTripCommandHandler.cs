using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Trip.CreateTrip;

internal sealed class CreateTripCommandHandler(
    ITransporterProfileRepository profileRepository,
    ITripListingRepository tripListingRepository,
    TimeProvider timeProvider,
    ILogger<CreateTripCommandHandler> logger)
    : IRequestHandler<CreateTripCommand, Result<TripListingResponse>>
{
    public async Task<Result<TripListingResponse>> Handle(
        CreateTripCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result<TripListingResponse>.Error(TripErrorCodes.ProfileNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (!TripDateRules.IsFutureDepartureDate(request.DepartureDateUtc, nowUtc))
        {
            return Result<TripListingResponse>.Error(TripErrorCodes.DepartureDateMustBeFuture);
        }

        var trip = TripListing.Create(
            Guid.NewGuid(),
            profile.Id,
            request.OriginCity,
            request.DestinationCity,
            request.DepartureDateUtc,
            request.AvailableCapacityKg,
            request.PriceGuideZar,
            nowUtc,
            request.Description);

        await tripListingRepository.AddAsync(trip, cancellationToken);
        await tripListingRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created trip listing {TripListingId} for profile {ProfileId}",
            trip.Id,
            profile.Id);

        return Result<TripListingResponse>.Success(TripMappings.ToResponse(trip));
    }
}
