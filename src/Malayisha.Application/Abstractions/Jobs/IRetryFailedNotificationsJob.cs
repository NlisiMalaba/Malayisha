namespace Malayisha.Application.Abstractions.Jobs;

public interface IRetryFailedNotificationsJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
