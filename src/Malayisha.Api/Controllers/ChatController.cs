using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Chat;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Chat.GetMessageHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class ChatController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}/messages")]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
    [ProducesResponseType(typeof(ChatMessageHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageHistory(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new GetMessageHistoryQuery(userId, id),
            cancellationToken);

        return ChatResultMapper.ToMessageHistoryResult(result);
    }
}
