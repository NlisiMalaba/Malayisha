using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Review.HideReview;

public sealed record HideReviewCommand(Guid ReviewId, Guid AdminUserId)
    : IRequest<Result<AdminReviewDto>>;

internal sealed class HideReviewCommandValidator : AbstractValidator<HideReviewCommand>
{
    public HideReviewCommandValidator()
    {
        RuleFor(command => command.ReviewId).NotEmpty();
        RuleFor(command => command.AdminUserId).NotEmpty();
    }
}

internal sealed class HideReviewCommandHandler(
    IReviewRepository reviewRepository,
    ITransporterProfileRepository transporterProfileRepository,
    IAuditLogRepository auditLogRepository,
    TimeProvider timeProvider,
    ILogger<HideReviewCommandHandler> logger)
    : IRequestHandler<HideReviewCommand, Result<AdminReviewDto>>
{
    public async Task<Result<AdminReviewDto>> Handle(
        HideReviewCommand request,
        CancellationToken cancellationToken)
    {
        var review = await reviewRepository.FindByIdForUpdateAsync(request.ReviewId, cancellationToken);
        if (review is null)
        {
            return Result<AdminReviewDto>.Error(ReviewErrorCodes.ReviewNotFound);
        }

        if (review.IsHidden)
        {
            return Result<AdminReviewDto>.Error(ReviewErrorCodes.ReviewAlreadyHidden);
        }

        var profile = await transporterProfileRepository.FindByIdForUpdateAsync(
            review.TransporterProfileId,
            cancellationToken);

        if (profile is null)
        {
            return Result<AdminReviewDto>.Error(ReviewErrorCodes.TransporterProfileNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var publicRatings = await reviewRepository.ListPublicRatingsByTransporterProfileIdAsync(
            profile.Id,
            cancellationToken);
        var averageRating = ReviewAverageCalculator.CalculateAfterHiding(publicRatings, review.Rating);

        review.SetVisibility(isHidden: true, nowUtc);
        profile.SetAverageRating(averageRating, nowUtc);

        await auditLogRepository.AddAsync(
            AuditLog.Create(
                Guid.NewGuid(),
                request.AdminUserId,
                ReviewAuditActions.Hidden,
                ReviewAuditActions.TargetType,
                review.Id,
                nowUtc),
            cancellationToken);

        await reviewRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Review {ReviewId} hidden by admin {AdminUserId}; transporter profile {ProfileId} average rating recalculated to {AverageRating}",
            review.Id,
            request.AdminUserId,
            profile.Id,
            profile.AverageRating);

        return Result<AdminReviewDto>.Success(ReviewMappings.ToAdminDto(review));
    }
}
