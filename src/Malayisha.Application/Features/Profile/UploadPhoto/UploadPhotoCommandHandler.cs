using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Abstractions.Storage;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Profile.UploadPhoto;

internal sealed class UploadPhotoCommandHandler(
    ITransporterProfileRepository profileRepository,
    IFileStorageService fileStorageService,
    TimeProvider timeProvider,
    ILogger<UploadPhotoCommandHandler> logger)
    : IRequestHandler<UploadPhotoCommand, Result<UploadProfilePhotoResponse>>
{
    public async Task<Result<UploadProfilePhotoResponse>> Handle(
        UploadPhotoCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await profileRepository.FindByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
        {
            return Result<UploadProfilePhotoResponse>.Error(ProfileErrorCodes.ProfileNotFound);
        }

        var upload = await fileStorageService.CreatePresignedPutUploadAsync(
            ProfileValidation.ProfilePhotoCategory,
            profile.Id,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var cdnUrl = upload.CdnUrl.ToString();
        profile.SetProfilePhotoUrl(cdnUrl, nowUtc);
        await profileRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Persisted profile photo CDN URL for profile {ProfileId} with object key {ObjectKey}",
            profile.Id,
            upload.ObjectKey);

        return Result<UploadProfilePhotoResponse>.Success(
            new UploadProfilePhotoResponse(
                upload.UploadUrl,
                upload.ObjectKey,
                upload.CdnUrl,
                upload.ExpiresAtUtc,
                cdnUrl));
    }
}
