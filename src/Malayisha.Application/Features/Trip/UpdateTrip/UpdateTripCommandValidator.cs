using FluentValidation;

namespace Malayisha.Application.Features.Trip.UpdateTrip;

internal sealed class UpdateTripCommandValidator : AbstractValidator<UpdateTripCommand>
{
    public UpdateTripCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.TripListingId)
            .NotEmpty();

        RuleFor(command => command.OriginCity)
            .NotEmpty()
            .MaximumLength(TripValidation.CityMaxLength);

        RuleFor(command => command.DestinationCity)
            .NotEmpty()
            .MaximumLength(TripValidation.CityMaxLength);

        RuleFor(command => command.AvailableCapacityKg)
            .GreaterThan(0);

        RuleFor(command => command.PriceGuideZar)
            .GreaterThan(0);

        RuleFor(command => command.Description)
            .MaximumLength(TripValidation.DescriptionMaxLength)
            .When(command => command.Description is not null);
    }
}
