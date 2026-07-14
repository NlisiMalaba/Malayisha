using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Profile.CreateProfile;

internal sealed class CreateProfileCommandHandler(
    ITransporterProfileRepository profileRepository,
    TimeProvider timeProvider,
    ILogger<CreateProfileCommandHandler> logger)
    : IRequestHandler<CreateProfileCommand, Result<TransporterProfileResponse>>
{
    public async Task<Result<TransporterProfileResponse>> Handle(
        CreateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (await profileRepository.ExistsForUserAsync(request.UserId, cancellationToken))
        {
            return Result<TransporterProfileResponse>.Error(ProfileErrorCodes.ProfileAlreadyExists);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var profile = TransporterProfile.Create(
            Guid.NewGuid(),
            request.UserId,
            request.DisplayName,
            request.RoutesServed,
            request.VehicleDescription,
            request.CapacityKg,
            nowUtc,
            request.ProfilePhotoUrl);

        await profileRepository.AddAsync(profile, cancellationToken);
        await profileRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created transporter profile {ProfileId} for user {UserId}",
            profile.Id,
            profile.UserId);

        return Result<TransporterProfileResponse>.Success(ProfileMappings.ToResponse(profile));
    }
}
