using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Verification;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Verification.ApplyForVerification;
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
}
