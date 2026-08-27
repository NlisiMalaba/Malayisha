using FluentValidation;

namespace Malayisha.Application.Features.Profile.GetMyProfile;

internal sealed class GetMyProfileQueryValidator : AbstractValidator<GetMyProfileQuery>
{
    public GetMyProfileQueryValidator()
    {
        RuleFor(query => query.UserId)
            .NotEmpty();
    }
}
