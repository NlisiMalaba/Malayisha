using FluentValidation;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.Auth.VerifyOtp;

internal sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .Matches(AuthValidation.PhoneNumberPattern);

        RuleFor(command => command.OtpCode)
            .NotEmpty()
            .Matches(@"^\d{6}$");

        RuleFor(command => command.Purpose)
            .IsInEnum();

        When(command => command.Purpose == OtpPurpose.Register, () =>
        {
            RuleFor(command => command.Role)
                .NotNull()
                .Must(role => AuthValidation.IsAllowedRegistrationRole(role!.Value))
                .WithErrorCode(AuthErrorCodes.InvalidRole);
        });

        When(command => command.Purpose == OtpPurpose.Login, () =>
        {
            RuleFor(command => command.Role)
                .Null();
        });
    }
}
