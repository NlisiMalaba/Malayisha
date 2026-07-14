using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Features.Trip.GetShareLink;

internal sealed class GetShareLinkQueryHandler(
    ITripListingRepository tripListingRepository,
    ITransporterProfileRepository profileRepository,
    IOptions<AppLinkOptions> appLinkOptions,
    ILogger<GetShareLinkQueryHandler> logger)
    : IRequestHandler<GetShareLinkQuery, Result<ShareLinkResponse>>
{
    public async Task<Result<ShareLinkResponse>> Handle(
        GetShareLinkQuery request,
        CancellationToken cancellationToken)
    {
        var trip = await tripListingRepository.FindByIdAsync(request.TripListingId, cancellationToken);
        if (trip is null || trip.IsDeleted)
        {
            return Result<ShareLinkResponse>.Error(TripErrorCodes.TripNotFound);
        }

        var profile = await profileRepository.FindByIdAsync(trip.TransporterProfileId, cancellationToken);
        if (profile is null)
        {
            return Result<ShareLinkResponse>.Error(TripErrorCodes.ProfileNotFound);
        }

        var url = WhatsAppShareLinkBuilder.BuildUrl(
            trip,
            profile,
            appLinkOptions.Value.TripDeepLinkBaseUrl);

        logger.LogInformation(
            "Generated WhatsApp share link for trip listing {TripListingId}",
            trip.Id);

        return Result<ShareLinkResponse>.Success(new ShareLinkResponse(url));
    }
}
