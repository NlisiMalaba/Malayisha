namespace Malayisha.Application.Features.Auth;

public static class AuthValidation
{
    public const string PhoneNumberPattern = @"^\+[1-9]\d{1,14}$";

    public static bool IsAllowedRegistrationRole(Domain.Enums.UserRole role) =>
        role is Domain.Enums.UserRole.Sender or Domain.Enums.UserRole.Transporter;
}
