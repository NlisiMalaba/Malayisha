using Malayisha.Application.Features.Auth;

namespace Malayisha.Api.Mapping;

internal static class AuthErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            AuthErrorCodes.PhoneAlreadyRegistered => StatusCodes.Status409Conflict,
            AuthErrorCodes.InvalidRole => StatusCodes.Status400BadRequest,
            AuthErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
            AuthErrorCodes.UserInactive => StatusCodes.Status403Forbidden,
            AuthErrorCodes.PhoneLockedOut => StatusCodes.Status429TooManyRequests,
            AuthErrorCodes.OtpSendRateLimited => StatusCodes.Status429TooManyRequests,
            AuthErrorCodes.InvalidOtp => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.OtpExpired => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.InvalidRefreshToken => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.RefreshTokenExpired => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.RefreshTokenRevoked => StatusCodes.Status401Unauthorized,
            AuthErrorCodes.RefreshTokenAlreadyUsed => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status400BadRequest
        };
}
