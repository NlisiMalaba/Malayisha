using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Trip.DeleteTrip;

internal sealed class DeleteTripCommandHandler(
    ITransporterProfileRepository profileRepository,
    ITripListingRepository tripListingRepository,
    TimeProvider timeProvider,
    ILogger<DeleteTripCommandHandler> logger)
    : IRequestHandler<DeleteTripCommand, Result>
{
    public async Task<Result> Handle(
        DeleteTripCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result.Error(TripErrorCodes.ProfileNotFound);
        }

        var trip = await tripListingRepository.FindByIdAsync(request.TripListingId, cancellationToken);
        if (trip is null || trip.IsDeleted)
        {
            return Result.Error(TripErrorCodes.TripNotFound);
        }

        if (trip.TransporterProfileId != profile.Id)
        {
            return Result.Error(TripErrorCodes.NotTripOwner);
        }

        if (await tripListingRepository.HasBlockingBookingsAsync(trip.Id, cancellationToken))
        {
            return Result.Error(TripErrorCodes.ActiveBookingsBlockDelete);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        trip.MarkDeleted(nowUtc);

        await tripListingRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted trip listing {TripListingId} for profile {ProfileId}",
            trip.Id,
            profile.Id);

        return Result.Success();
    }
}
