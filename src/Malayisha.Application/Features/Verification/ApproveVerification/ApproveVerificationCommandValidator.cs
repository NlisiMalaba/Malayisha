using FluentValidation;

namespace Malayisha.Application.Features.Verification.ApproveVerification;

internal sealed class ApproveVerificationCommandValidator : AbstractValidator<ApproveVerificationCommand>
{
    public ApproveVerificationCommandValidator()
    {
        RuleFor(command => command.VerificationId)
            .NotEmpty();

        RuleFor(command => command.AdminUserId)
            .NotEmpty();
    }
}
