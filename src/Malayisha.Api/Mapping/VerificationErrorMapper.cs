using Malayisha.Application.Features.Verification;

namespace Malayisha.Api.Mapping;

internal static class VerificationErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            VerificationErrorCodes.ActiveVerificationExists => StatusCodes.Status409Conflict,
            VerificationErrorCodes.ProfileNotFound => StatusCodes.Status404NotFound,
            VerificationErrorCodes.VerificationNotFound => StatusCodes.Status404NotFound,
            VerificationErrorCodes.InvalidVerificationStatus => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };
}
