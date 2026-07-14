using FluentValidation;

namespace Malayisha.Application.Features.Profile.CreateProfile;

internal sealed class CreateProfileCommandValidator : AbstractValidator<CreateProfileCommand>
{
    public CreateProfileCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.DisplayName)
            .NotEmpty()
            .MaximumLength(ProfileValidation.DisplayNameMaxLength);

        RuleFor(command => command.RoutesServed)
            .NotNull()
            .Must(routes => routes.Count > 0)
            .WithMessage("At least one route is required.")
            .Must(routes => routes.Count <= ProfileValidation.MaxRoutes)
            .WithMessage($"A maximum of {ProfileValidation.MaxRoutes} routes is allowed.");

        RuleForEach(command => command.RoutesServed)
            .NotEmpty()
            .MaximumLength(ProfileValidation.RouteMaxLength);

        RuleFor(command => command.VehicleDescription)
            .NotEmpty()
            .MaximumLength(ProfileValidation.VehicleDescriptionMaxLength);

        RuleFor(command => command.CapacityKg)
            .GreaterThan(0);

        RuleFor(command => command.ProfilePhotoUrl)
            .MaximumLength(ProfileValidation.ProfilePhotoUrlMaxLength)
            .When(command => command.ProfilePhotoUrl is not null);
    }
}
