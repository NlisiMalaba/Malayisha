using FluentValidation;

namespace Malayisha.Application.Features.Trip.SearchTrips;

internal sealed class SearchTripsQueryValidator : AbstractValidator<SearchTripsQuery>
{
    public SearchTripsQueryValidator()
    {
        RuleFor(query => query.OriginCity)
            .NotEmpty()
            .MaximumLength(TripValidation.CityMaxLength);

        RuleFor(query => query.DestinationCity)
            .NotEmpty()
            .MaximumLength(TripValidation.CityMaxLength);

        RuleFor(query => query.MaxPriceZar)
            .GreaterThan(0)
            .When(query => query.MaxPriceZar.HasValue);

        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, TripValidation.MaxPageSize);
    }
}
