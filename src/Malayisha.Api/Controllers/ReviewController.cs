using Malayisha.Api.Authorization;
using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Review;
using Malayisha.Api.Mapping;
using Malayisha.Application.Features.Review.CreateReview;
using Malayisha.Application.Features.Review.GetTransporterReviews;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthPolicies.SenderOnly)]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(new ErrorResponse("Unauthorized"));
        }

        var result = await mediator.Send(
            new CreateReviewCommand(
                userId,
                request.BookingId,
                request.Rating,
                request.Comment),
            cancellationToken);

        return ReviewResultMapper.ToCreatedResult(result);
    }

    [HttpGet("transporter/{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TransporterReviewsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransporterReviews(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTransporterReviewsQuery(id), cancellationToken);
        return ReviewResultMapper.ToTransporterReviewsResult(result);
    }
}
