using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Notification;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class NotificationResultMapper
{
    public static IActionResult ToPreferencesResult(Result<NotificationPreferencesResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    private static NotificationPreferencesDto ToDto(NotificationPreferencesResponse preferences) =>
        new(preferences.MarketingNotificationsOptIn);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = NotificationErrorMapper.ToStatusCode(errorCode)
        };
}
