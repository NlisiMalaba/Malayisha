using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Verification;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Verification.ApplyForVerification;
using Malayisha.Application.Features.Verification.ApproveVerification;
using Malayisha.Application.Features.Verification.GetPendingVerifications;
using Malayisha.Application.Features.Verification.RejectVerification;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/verification")]
public sealed class VerificationController(IMediator mediator) : ControllerBase
{
    [HttpPost("apply")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(VerificationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Apply(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new ApplyForVerificationCommand(userId), cancellationToken);
        return VerificationResultMapper.ToCreatedResult(result);
    }

    [HttpGet("pending")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(IReadOnlyList<PendingVerificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPending(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPendingVerificationsQuery(), cancellationToken);
        return VerificationResultMapper.ToPendingListResult(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(VerificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new ApproveVerificationCommand(id, adminUserId),
            cancellationToken);

        return VerificationResultMapper.ToResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = AuthPolicies.AdminOnly)]
    [ProducesResponseType(typeof(VerificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectVerificationRequest? request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var adminUserId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new RejectVerificationCommand(id, adminUserId, request?.RejectionReason),
            cancellationToken);

        return VerificationResultMapper.ToResult(result);
    }
}
