using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Trip;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Trip.CreateTrip;
using Malayisha.Application.Features.Trip.DeleteTrip;
using Malayisha.Application.Features.Trip.GetShareLink;
using Malayisha.Application.Features.Trip.SearchTrips;
using Malayisha.Application.Features.Trip.UpdateTrip;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/trips")]
public sealed class TripController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(TripListingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTripRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CreateTripCommand(
                userId,
                request.OriginCity,
                request.DestinationCity,
                request.DepartureDateUtc,
                request.AvailableCapacityKg,
                request.PriceGuideZar,
                request.Description),
            cancellationToken);

        return TripResultMapper.ToCreatedResult(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(typeof(TripListingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTripRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new UpdateTripCommand(
                userId,
                id,
                request.OriginCity,
                request.DestinationCity,
                request.DepartureDateUtc,
                request.AvailableCapacityKg,
                request.PriceGuideZar,
                request.Description),
            cancellationToken);

        return TripResultMapper.ToListingResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthPolicies.TransporterOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(new DeleteTripCommand(userId, id), cancellationToken);
        return TripResultMapper.ToDeleteResult(result);
    }

    [HttpGet("search")]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(typeof(TripSearchPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchTripsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SearchTripsQuery(
                request.OriginCity,
                request.DestinationCity,
                request.DepartureDate,
                request.MaxPriceZar,
                request.VerifiedOnly,
                request.Page,
                request.PageSize),
            cancellationToken);

        return TripResultMapper.ToSearchResult(result);
    }

    [HttpGet("{id:guid}/share-link")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ShareLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetShareLink(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetShareLinkQuery(id), cancellationToken);
        return TripResultMapper.ToShareLinkResult(result);
    }
}
