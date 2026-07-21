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
            ? new OkObjectResult(ToPreferencesDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToDeviceTokenResult(Result<PushDeviceTokenResponse> result) =>
        result.IsSuccess && result.Value is not null
            ? new OkObjectResult(ToDeviceTokenDto(result.Value))
            : ToErrorResult(result.ErrorCode);

    private static NotificationPreferencesDto ToPreferencesDto(NotificationPreferencesResponse preferences) =>
        new(preferences.MarketingNotificationsOptIn);

    private static PushDeviceTokenDto ToDeviceTokenDto(PushDeviceTokenResponse response) =>
        new(response.Registered);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = NotificationErrorMapper.ToStatusCode(errorCode)
        };
}
