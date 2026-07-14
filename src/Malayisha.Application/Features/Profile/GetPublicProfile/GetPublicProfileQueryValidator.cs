using FluentValidation;

namespace Malayisha.Application.Features.Profile.GetPublicProfile;

internal sealed class GetPublicProfileQueryValidator : AbstractValidator<GetPublicProfileQuery>
{
    public GetPublicProfileQueryValidator()
    {
        RuleFor(query => query.ProfileId)
            .NotEmpty();
    }
}
