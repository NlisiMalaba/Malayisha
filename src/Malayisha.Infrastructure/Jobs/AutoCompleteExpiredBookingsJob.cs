using Malayisha.Application.Abstractions.Jobs;
using Microsoft.Extensions.Logging;

namespace Malayisha.Infrastructure.Jobs;

internal sealed class AutoCompleteExpiredBookingsJob(
    ILogger<AutoCompleteExpiredBookingsJob> logger) : IAutoCompleteExpiredBookingsJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Business logic is implemented in task 8.3 (Delivered → Completed after 48h + commission).
        logger.LogDebug("AutoCompleteExpiredBookingsJob invoked; awaiting booking workflow wiring.");
        return Task.CompletedTask;
    }
}
