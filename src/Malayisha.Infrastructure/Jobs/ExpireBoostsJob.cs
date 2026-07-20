using Malayisha.Application.Abstractions.Jobs;
using Malayisha.Application.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Jobs;

internal sealed class ExpireBoostsJob(
    ITripListingRepository tripListingRepository,
    TimeProvider timeProvider,
    ILogger<ExpireBoostsJob> logger) : IExpireBoostsJob
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var expiredTrips = await tripListingRepository.ListExpiredBoostedForUpdateAsync(
            nowUtc,
            cancellationToken);

        if (expiredTrips.Count == 0)
        {
            logger.LogDebug("ExpireBoostsJob found no expired boosts at {NowUtc}", nowUtc);
            return;
        }

        var clearedCount = 0;

        foreach (var trip in expiredTrips)
        {
            if (!trip.IsBoosted
                || trip.BoostEndAtUtc is null
                || trip.BoostEndAtUtc > nowUtc)
            {
                continue;
            }

            trip.ClearBoost(nowUtc);
            clearedCount++;
        }

        if (clearedCount > 0)
        {
            await tripListingRepository.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "ExpireBoostsJob cleared {ClearedCount} expired boosts out of {CandidateCount} candidates at {NowUtc}",
            clearedCount,
            expiredTrips.Count,
            nowUtc);
    }
}
