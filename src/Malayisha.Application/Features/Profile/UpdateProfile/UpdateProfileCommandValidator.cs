using FluentValidation;

namespace Malayisha.Application.Features.Profile.UpdateProfile;

internal sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
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
    }
}
