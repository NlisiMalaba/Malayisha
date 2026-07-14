using FluentValidation;

namespace Malayisha.Application.Features.Verification.RejectVerification;

internal sealed class RejectVerificationCommandValidator : AbstractValidator<RejectVerificationCommand>
{
    public RejectVerificationCommandValidator()
    {
        RuleFor(command => command.VerificationId)
            .NotEmpty();

        RuleFor(command => command.AdminUserId)
            .NotEmpty();

        RuleFor(command => command.RejectionReason)
            .MaximumLength(VerificationValidation.RejectionReasonMaxLength)
            .When(command => command.RejectionReason is not null);
    }
}
