using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Review.GetAllReviews;

public sealed record GetAllReviewsQuery : IRequest<Result<IReadOnlyList<AdminReviewDto>>>;

internal sealed class GetAllReviewsQueryHandler(
    IReviewRepository reviewRepository,
    ILogger<GetAllReviewsQueryHandler> logger)
    : IRequestHandler<GetAllReviewsQuery, Result<IReadOnlyList<AdminReviewDto>>>
{
    public async Task<Result<IReadOnlyList<AdminReviewDto>>> Handle(
        GetAllReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var reviews = await reviewRepository.ListAllOrderedByCreatedAtDescAsync(cancellationToken);

        var dtos = reviews
            .Select(ReviewMappings.ToAdminDto)
            .ToList();

        logger.LogInformation("Retrieved {ReviewCount} reviews for admin listing", dtos.Count);

        return Result<IReadOnlyList<AdminReviewDto>>.Success(dtos);
    }
}
