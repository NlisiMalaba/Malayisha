using Malayisha.Application.Abstractions.Jobs;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Jobs;

internal sealed class ExpireBoostsJob(ILogger<ExpireBoostsJob> logger) : IExpireBoostsJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Business logic is implemented in task 11.6 (clear expired trip listing boosts).
        logger.LogDebug("ExpireBoostsJob invoked; awaiting boost workflow wiring.");
        return Task.CompletedTask;
    }
}
