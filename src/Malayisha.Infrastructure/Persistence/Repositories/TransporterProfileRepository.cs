using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class TransporterProfileRepository(MalayishaDbContext dbContext) : ITransporterProfileRepository
{
    public Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
        dbContext.TransporterProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.Id == profileId, cancellationToken);

    public Task<TransporterProfile?> FindByIdForUpdateAsync(
        Guid profileId,
        CancellationToken cancellationToken = default) =>
        dbContext.TransporterProfiles
            .FirstOrDefaultAsync(profile => profile.Id == profileId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, TransporterProfile>> FindByIdsAsync(
        IEnumerable<Guid> profileIds,
        CancellationToken cancellationToken = default)
    {
        var ids = profileIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, TransporterProfile>();
        }

        var profiles = await dbContext.TransporterProfiles
            .AsNoTracking()
            .Where(profile => ids.Contains(profile.Id))
            .ToListAsync(cancellationToken);

        return profiles.ToDictionary(profile => profile.Id);
    }

    public Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.TransporterProfiles
            .FirstOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

    public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.TransporterProfiles
            .AsNoTracking()
            .AnyAsync(profile => profile.UserId == userId, cancellationToken);

    public async Task AddAsync(TransporterProfile profile, CancellationToken cancellationToken = default)
    {
        await dbContext.TransporterProfiles.AddAsync(profile, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
