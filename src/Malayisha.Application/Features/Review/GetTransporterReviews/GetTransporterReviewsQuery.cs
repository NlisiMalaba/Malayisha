using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Review.GetTransporterReviews;

public sealed record GetTransporterReviewsQuery(Guid TransporterProfileId)
    : IRequest<Result<IReadOnlyList<ReviewDto>>>;

internal sealed class GetTransporterReviewsQueryValidator : AbstractValidator<GetTransporterReviewsQuery>
{
    public GetTransporterReviewsQueryValidator()
    {
        RuleFor(query => query.TransporterProfileId).NotEmpty();
    }
}

internal sealed class GetTransporterReviewsQueryHandler(
    ITransporterProfileRepository transporterProfileRepository,
    IReviewRepository reviewRepository,
    ILogger<GetTransporterReviewsQueryHandler> logger)
    : IRequestHandler<GetTransporterReviewsQuery, Result<IReadOnlyList<ReviewDto>>>
{
    public async Task<Result<IReadOnlyList<ReviewDto>>> Handle(
        GetTransporterReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await transporterProfileRepository.FindByIdAsync(
            request.TransporterProfileId,
            cancellationToken);

        if (profile is null)
        {
            return Result<IReadOnlyList<ReviewDto>>.Error(ReviewErrorCodes.TransporterProfileNotFound);
        }

        var reviews = await reviewRepository.ListPublicByTransporterProfileIdAsync(
            request.TransporterProfileId,
            cancellationToken);

        var dtos = reviews
            .Select(ReviewMappings.ToDto)
            .ToList();

        logger.LogInformation(
            "Retrieved {ReviewCount} public reviews for transporter profile {ProfileId}",
            dtos.Count,
            request.TransporterProfileId);

        return Result<IReadOnlyList<ReviewDto>>.Success(dtos);
    }
}
