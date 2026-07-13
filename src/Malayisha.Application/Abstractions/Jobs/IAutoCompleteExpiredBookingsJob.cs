namespace Malayisha.Application.Abstractions.Jobs;

public interface IAutoCompleteExpiredBookingsJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
