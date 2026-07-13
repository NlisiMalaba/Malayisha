namespace Malayisha.Application.Abstractions.Jobs;

public interface IExpireBoostsJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
