using FluentValidation;

namespace Malayisha.Application.Features.Verification.ApplyForVerification;

internal sealed class ApplyForVerificationCommandValidator : AbstractValidator<ApplyForVerificationCommand>
{
    public ApplyForVerificationCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();
    }
}
