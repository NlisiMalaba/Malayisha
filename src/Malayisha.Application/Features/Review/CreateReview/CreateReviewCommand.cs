using FluentValidation;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Review.CreateReview;

[AuthorizeRoles(UserRole.Sender)]
public sealed record CreateReviewCommand(
    Guid SenderId,
    Guid BookingId,
    int Rating,
    string? Comment) : IRequest<Result<ReviewDto>>;

internal sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.SenderId).NotEmpty();
        RuleFor(command => command.BookingId).NotEmpty();
        RuleFor(command => command.Rating)
            .InclusiveBetween(ReviewConstants.MinRating, ReviewConstants.MaxRating)
            .WithErrorCode(ReviewErrorCodes.InvalidRating);
        RuleFor(command => command.Comment)
            .MaximumLength(ReviewConstants.MaxCommentLength);
    }
}

internal sealed class CreateReviewCommandHandler(
    IBookingRepository bookingRepository,
    ITripListingRepository tripListingRepository,
    ITransporterProfileRepository transporterProfileRepository,
    IReviewRepository reviewRepository,
    TimeProvider timeProvider,
    ILogger<CreateReviewCommandHandler> logger) : IRequestHandler<CreateReviewCommand, Result<ReviewDto>>
{
    public async Task<Result<ReviewDto>> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.FindByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result<ReviewDto>.Error(ReviewErrorCodes.BookingNotFound);
        }

        if (booking.SenderId != request.SenderId)
        {
            return Result<ReviewDto>.Error(ReviewErrorCodes.NotBookingSender);
        }

        if (booking.Status != BookingStatus.Completed)
        {
            return Result<ReviewDto>.Error(ReviewErrorCodes.BookingNotCompleted);
        }

        if (await reviewRepository.ExistsByBookingIdAsync(request.BookingId, cancellationToken))
        {
            return Result<ReviewDto>.Error(ReviewErrorCodes.ReviewAlreadyExists);
        }

        var tripListing = await tripListingRepository.FindByIdAsync(booking.TripListingId, cancellationToken);
        if (tripListing is null || tripListing.IsDeleted)
        {
            return Result<ReviewDto>.Error(ReviewErrorCodes.TripNotFound);
        }

        var profile = await transporterProfileRepository.FindByIdForUpdateAsync(
            tripListing.TransporterProfileId,
            cancellationToken);

        if (profile is null)
        {
            return Result<ReviewDto>.Error(ReviewErrorCodes.TransporterProfileNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;

        var priorRatings = await reviewRepository.ListPublicRatingsByTransporterProfileIdAsync(
            profile.Id,
            cancellationToken);
        var averageRating = ReviewAverageCalculator.Calculate(priorRatings.Append(request.Rating));

        var review = Domain.Entities.Review.Create(
            Guid.NewGuid(),
            booking.Id,
            request.SenderId,
            profile.Id,
            request.Rating,
            request.Comment,
            nowUtc);

        await reviewRepository.AddAsync(review, cancellationToken);
        profile.SetAverageRating(averageRating, nowUtc);

        await reviewRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Review {ReviewId} created for booking {BookingId} by sender {SenderId}; transporter profile {ProfileId} average rating updated to {AverageRating}",
            review.Id,
            review.BookingId,
            review.SenderId,
            profile.Id,
            averageRating);

        return Result<ReviewDto>.Success(ReviewMappings.ToDto(review));
    }
}
