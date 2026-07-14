using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface ITransporterProfileRepository
{
    Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<TransporterProfile?> FindByIdForUpdateAsync(Guid profileId, CancellationToken cancellationToken = default);

    Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(TransporterProfile profile, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
