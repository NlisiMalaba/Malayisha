using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Profile.UpdateProfile;

internal sealed class UpdateProfileCommandHandler(
    ITransporterProfileRepository profileRepository,
    TimeProvider timeProvider,
    ILogger<UpdateProfileCommandHandler> logger)
    : IRequestHandler<UpdateProfileCommand, Result<TransporterProfileResponse>>
{
    public async Task<Result<TransporterProfileResponse>> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result<TransporterProfileResponse>.Error(ProfileErrorCodes.ProfileNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        profile.Update(
            request.DisplayName,
            request.RoutesServed,
            request.VehicleDescription,
            request.CapacityKg,
            nowUtc);

        await profileRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated transporter profile {ProfileId} for user {UserId}",
            profile.Id,
            profile.UserId);

        return Result<TransporterProfileResponse>.Success(ProfileMappings.ToResponse(profile));
    }
}
