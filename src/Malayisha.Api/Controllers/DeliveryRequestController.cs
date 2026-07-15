using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.DeliveryRequest;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.DeliveryRequest.CancelDeliveryRequest;
using Malayisha.Application.Features.DeliveryRequest.CreateDeliveryRequest;
using Malayisha.Application.Features.DeliveryRequest.ListDeliveryRequests;
using Malayisha.Application.Features.DeliveryRequest.UpdateDeliveryRequest;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class DeliveryRequestController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(typeof(DeliveryRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeliveryRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CreateDeliveryRequestCommand(
                userId,
                request.OriginCity,
                request.DestinationCity,
                request.RequiredDateUtc,
                request.WeightKg,
                request.SizeDescription,
                request.GoodsDescription),
            cancellationToken);

        return DeliveryRequestResultMapper.ToCreatedResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(typeof(DeliveryRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDeliveryRequestRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new UpdateDeliveryRequestCommand(
                userId,
                id,
                request.OriginCity,
                request.DestinationCity,
                request.RequiredDateUtc,
                request.WeightKg,
                request.SizeDescription,
                request.GoodsDescription),
            cancellationToken);

        return DeliveryRequestResultMapper.ToResponseResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CancelDeliveryRequestCommand(userId, id),
            cancellationToken);

        return DeliveryRequestResultMapper.ToCancelResult(result);
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(DeliveryRequestPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] ListDeliveryRequestsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListDeliveryRequestsQuery(request.Page, request.PageSize),
            cancellationToken);

        return DeliveryRequestResultMapper.ToListResult(result);
    }
}
