using FluentValidation;

namespace Malayisha.Application.Features.Profile.UploadPhoto;

internal sealed class UploadPhotoCommandValidator : AbstractValidator<UploadPhotoCommand>
{
    public UploadPhotoCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.ContentType)
            .NotEmpty()
            .Must(contentType => ProfileValidation.AllowedPhotoContentTypes.Contains(contentType))
            .WithErrorCode(ProfileErrorCodes.InvalidProfilePhoto)
            .WithMessage("Profile photo content type must be image/jpeg, image/png, or image/webp.");
    }
}
