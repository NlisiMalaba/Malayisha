using Malayisha.Application.Common;
using Microsoft.AspNetCore.Http;

namespace Malayisha.Api.Mapping;

internal static class ApplicationErrorMapper
{
    public static int? TryGetPipelineStatusCode(string? errorCode) =>
        errorCode switch
        {
            ApplicationErrorCodes.ValidationFailed => StatusCodes.Status400BadRequest,
            ApplicationErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ApplicationErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => null
        };
}
