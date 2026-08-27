using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Profile.GetMyProfile;

internal sealed class GetMyProfileQueryHandler(
    ITransporterProfileRepository profileRepository,
    ILogger<GetMyProfileQueryHandler> logger)
    : IRequestHandler<GetMyProfileQuery, Result<TransporterProfileResponse>>
{
    public async Task<Result<TransporterProfileResponse>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result<TransporterProfileResponse>.Error(ProfileErrorCodes.ProfileNotFound);
        }

        logger.LogInformation(
            "Loaded transporter profile {ProfileId} for user {UserId}",
            profile.Id,
            request.UserId);

        return Result<TransporterProfileResponse>.Success(ProfileMappings.ToResponse(profile));
    }
}
