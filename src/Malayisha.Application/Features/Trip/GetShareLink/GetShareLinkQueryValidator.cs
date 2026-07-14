using FluentValidation;

namespace Malayisha.Application.Features.Trip.GetShareLink;

internal sealed class GetShareLinkQueryValidator : AbstractValidator<GetShareLinkQuery>
{
    public GetShareLinkQueryValidator()
    {
        RuleFor(query => query.TripListingId)
            .NotEmpty();
    }
}
