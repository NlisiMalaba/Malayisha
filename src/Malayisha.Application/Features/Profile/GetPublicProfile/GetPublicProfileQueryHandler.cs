using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Profile.GetPublicProfile;

internal sealed class GetPublicProfileQueryHandler(ITransporterProfileRepository profileRepository)
    : IRequestHandler<GetPublicProfileQuery, Result<PublicTransporterProfileResponse>>
{
    public async Task<Result<PublicTransporterProfileResponse>> Handle(
        GetPublicProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByIdAsync(request.ProfileId, cancellationToken);
        if (profile is null)
        {
            return Result<PublicTransporterProfileResponse>.Error(ProfileErrorCodes.ProfileNotFound);
        }

        return Result<PublicTransporterProfileResponse>.Success(ProfileMappings.ToPublicResponse(profile));
    }
}
