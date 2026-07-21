using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Booking;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Booking.CancelBooking;
using Malayisha.Application.Features.Booking.CompleteBooking;
using Malayisha.Application.Features.Booking.ConfirmBooking;
using Malayisha.Application.Features.Booking.CreateBooking;
using Malayisha.Application.Features.Booking.GetBooking;
using Malayisha.Application.Features.Booking.ListBookings;
using Malayisha.Application.Features.Booking.MarkDelivered;
using Malayisha.Application.Features.Booking.MarkInTransit;
using Malayisha.Application.Features.Booking.QuoteBooking;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
    [ProducesResponseType(typeof(BookingPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] ListBookingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new ListBookingsQuery(userId, request.Page, request.PageSize),
            cancellationToken);

        return BookingResultMapper.ToListResult(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new GetBookingQuery(userId, id), cancellationToken);
        return BookingResultMapper.ToBookingResult(result);
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(typeof(BookingCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CreateBookingCommand(
                userId,
                request.TripListingId,
                request.DeliveryRequestId,
                request.Message),
            cancellationToken);

        return BookingResultMapper.ToCreatedResult(result);
    }

    [HttpPost("{id:guid}/quote")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Quote(
        Guid id,
        [FromBody] QuoteBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new QuoteBookingCommand(userId, id, request.QuotedPriceZar),
            cancellationToken);

        return BookingResultMapper.ToActionResult(result);
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(
        Guid id,
        [FromBody] ConfirmBookingRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new ConfirmBookingCommand(userId, id, request.AgreedPriceZar),
            cancellationToken);

        return BookingResultMapper.ToActionResult(result);
    }

    [HttpPost("{id:guid}/in-transit")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkInTransit(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new MarkInTransitCommand(userId, id), cancellationToken);
        return BookingResultMapper.ToActionResult(result);
    }

    [HttpPost("{id:guid}/delivered")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new MarkDeliveredCommand(userId, id), cancellationToken);
        return BookingResultMapper.ToActionResult(result);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CompleteBookingCommand(userId, id, UserRole.Sender),
            cancellationToken);

        return BookingResultMapper.ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.SenderOrTransporter)]
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

        var actorRole = User.IsInRole(UserRole.Transporter.ToString())
            ? UserRole.Transporter
            : UserRole.Sender;

        var result = await mediator.Send(
            new CancelBookingCommand(userId, id, actorRole),
            cancellationToken);

        return BookingResultMapper.ToActionResult(result);
    }
}
