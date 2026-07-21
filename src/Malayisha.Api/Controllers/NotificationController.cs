using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Notification;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Notifications.GetNotificationPreferences;
using Malayisha.Application.Features.Notifications.RegisterPushDeviceToken;
using Malayisha.Application.Features.Notifications.UpdateNotificationPreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationController(IMediator mediator) : ControllerBase
{
    [HttpGet("preferences")]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new GetNotificationPreferencesQuery(userId), cancellationToken);
        return NotificationResultMapper.ToPreferencesResult(result);
    }

    [HttpPut("preferences")]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
    [ProducesResponseType(typeof(NotificationPreferencesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new UpdateNotificationPreferencesCommand(userId, request.MarketingNotificationsOptIn),
            cancellationToken);

        return NotificationResultMapper.ToPreferencesResult(result);
    }

    [HttpPut("device-token")]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
    [ProducesResponseType(typeof(PushDeviceTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegisterDeviceToken(
        [FromBody] RegisterPushDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new RegisterPushDeviceTokenCommand(userId, request.DeviceToken),
            cancellationToken);

        return NotificationResultMapper.ToDeviceTokenResult(result);
    }
}
