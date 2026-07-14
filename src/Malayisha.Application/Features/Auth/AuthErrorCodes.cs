namespace Malayisha.Application.Features.Auth;

public static class AuthErrorCodes
{
    public const string PhoneLockedOut = "PhoneLockedOut";
    public const string InvalidOtp = "InvalidOtp";
    public const string OtpExpired = "OtpExpired";
    public const string UserNotFound = "UserNotFound";
    public const string PhoneAlreadyRegistered = "PhoneAlreadyRegistered";
    public const string UserInactive = "UserInactive";
    public const string InvalidRole = "InvalidRole";
    public const string InvalidRefreshToken = "InvalidRefreshToken";
    public const string RefreshTokenExpired = "RefreshTokenExpired";
    public const string RefreshTokenRevoked = "RefreshTokenRevoked";
    public const string RefreshTokenAlreadyUsed = "RefreshTokenAlreadyUsed";
}
