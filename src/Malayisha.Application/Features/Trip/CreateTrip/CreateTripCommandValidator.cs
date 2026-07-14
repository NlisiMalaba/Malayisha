using FluentValidation;

namespace Malayisha.Application.Features.Trip.CreateTrip;

internal sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(command => command.UserId)
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
