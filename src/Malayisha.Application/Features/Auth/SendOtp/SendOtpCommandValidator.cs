using FluentValidation;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Features.Auth.SendOtp;

internal sealed class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .Matches(AuthValidation.PhoneNumberPattern);

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
