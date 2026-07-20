using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Review;
using Malayisha.Application.Common;
using ApplicationReviewDto = Malayisha.Application.Features.Review.ReviewDto;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class ReviewResultMapper
{
    public static IActionResult ToCreatedResult(Result<ApplicationReviewDto> result) =>
        result.IsSuccess && result.Value is not null
            ? new ObjectResult(ToDto(result.Value)) { StatusCode = StatusCodes.Status201Created }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToTransporterReviewsResult(Result<IReadOnlyList<ApplicationReviewDto>> result) =>
        result.IsSuccess
            ? new OkObjectResult(new TransporterReviewsResponse(
                (result.Value ?? []).Select(ToDto).ToArray()))
            : ToErrorResult(result.ErrorCode);

    private static ReviewDto ToDto(ApplicationReviewDto review) =>
        new(
            review.Id,
            review.BookingId,
            review.SenderId,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = ReviewErrorMapper.ToStatusCode(errorCode)
        };
}
