using Malayisha.Application.Abstractions.Jobs;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Booking.CompleteBooking;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Options;
using Malayisha.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Malayisha.Infrastructure.Jobs;

internal sealed class AutoCompleteExpiredBookingsJob(
    IBookingRepository bookingRepository,
    ICommissionRecordRepository commissionRecordRepository,
    MalayishaDbContext dbContext,
    IMediator mediator,
    TimeProvider timeProvider,
    IOptions<BookingWorkflowOptions> bookingWorkflowOptions,
    ILogger<AutoCompleteExpiredBookingsJob> logger) : IAutoCompleteExpiredBookingsJob
{
    private static readonly Guid SystemActorId = Guid.Parse("f9d7557c-7e0a-40f2-9901-1e9290289c53");

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var options = bookingWorkflowOptions.Value;
        if (options.AutoCompleteAfterHours <= 0)
        {
            logger.LogWarning(
                "AutoCompleteExpiredBookingsJob skipped because AutoCompleteAfterHours is invalid: {Hours}",
                options.AutoCompleteAfterHours);
            return;
        }

        if (options.CommissionRate <= 0)
        {
            logger.LogWarning(
                "AutoCompleteExpiredBookingsJob skipped because CommissionRate is invalid: {CommissionRate}",
                options.CommissionRate);
            return;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var deliveredBeforeUtc = nowUtc.AddHours(-options.AutoCompleteAfterHours);
        var candidates = await bookingRepository.ListDeliveredBeforeAsync(deliveredBeforeUtc, cancellationToken);

        var completedCount = 0;
        var skippedCount = 0;

        foreach (var booking in candidates)
        {
            if (await commissionRecordRepository.ExistsForBookingAsync(booking.Id, cancellationToken))
            {
                skippedCount++;
                continue;
            }

            if (!booking.AgreedPriceZar.HasValue || booking.AgreedPriceZar.Value <= 0)
            {
                logger.LogWarning(
                    "Skipping booking {BookingId}; agreed price missing or invalid",
                    booking.Id);
                skippedCount++;
                continue;
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var completionResult = await mediator.Send(
                new CompleteBookingCommand(
                    SystemActorId,
                    booking.Id,
                    UserRole.Admin,
                    IsSystemAction: true),
                cancellationToken);

            if (completionResult.IsError)
            {
                logger.LogWarning(
                    "Failed to auto-complete booking {BookingId}; error {ErrorCode}",
                    booking.Id,
                    completionResult.ErrorCode);
                await transaction.RollbackAsync(cancellationToken);
                skippedCount++;
                continue;
            }

            if (await commissionRecordRepository.ExistsForBookingAsync(booking.Id, cancellationToken))
            {
                await transaction.CommitAsync(cancellationToken);
                skippedCount++;
                continue;
            }

            var commissionRecord = CommissionRecord.Create(
                Guid.NewGuid(),
                booking.Id,
                booking.TransporterId,
                booking.AgreedPriceZar.Value,
                options.CommissionRate,
                nowUtc);

            await commissionRecordRepository.AddAsync(commissionRecord, cancellationToken);
            await commissionRecordRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            completedCount++;
        }

        logger.LogInformation(
            "AutoCompleteExpiredBookingsJob processed {CandidateCount} bookings; completed {CompletedCount}, skipped {SkippedCount}",
            candidates.Count,
            completedCount,
            skippedCount);
    }
}
