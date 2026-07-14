using Malayisha.Api.Contracts.Auth;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Auth;
using Malayisha.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class AuthResultMapper
{
    public static IActionResult ToSendOtpResult(Result result) =>
        result.IsSuccess
            ? new OkObjectResult(new OtpSentResponse("OtpSent"))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToSessionResult(Result<AuthSessionResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    private static AuthSessionDto ToDto(AuthSessionResponse session) =>
        new(
            session.AccessToken,
            session.RefreshToken,
            session.ExpiresIn,
            session.UserId,
            session.Role.ToString(),
            session.PhoneNumber);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = AuthErrorMapper.ToStatusCode(errorCode)
        };
}
