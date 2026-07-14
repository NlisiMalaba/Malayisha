using Malayisha.Application.Features.Profile;

namespace Malayisha.Api.Mapping;

internal static class ProfileErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            ProfileErrorCodes.ProfileAlreadyExists => StatusCodes.Status409Conflict,
            ProfileErrorCodes.ProfileNotFound => StatusCodes.Status404NotFound,
            ProfileErrorCodes.InvalidProfilePhoto => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
}
