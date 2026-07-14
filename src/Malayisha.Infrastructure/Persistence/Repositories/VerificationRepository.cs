using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class VerificationRepository(MalayishaDbContext dbContext) : IVerificationRepository
{
    public Task<Verification?> FindByIdAsync(Guid verificationId, CancellationToken cancellationToken = default) =>
        dbContext.Verifications
            .FirstOrDefaultAsync(verification => verification.Id == verificationId, cancellationToken);

    public Task<bool> HasActiveForProfileAsync(
        Guid transporterProfileId,
        CancellationToken cancellationToken = default) =>
        dbContext.Verifications
            .AsNoTracking()
            .AnyAsync(
                verification => verification.TransporterProfileId == transporterProfileId
                    && (verification.Status == VerificationStatus.Pending
                        || verification.Status == VerificationStatus.Approved),
                cancellationToken);

    public async Task AddAsync(Verification verification, CancellationToken cancellationToken = default)
    {
        await dbContext.Verifications.AddAsync(verification, cancellationToken);
    }

    public async Task<IReadOnlyList<Verification>> ListPendingOrderedBySubmittedAtAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Verifications
            .AsNoTracking()
            .Where(verification => verification.Status == VerificationStatus.Pending)
            .OrderBy(verification => verification.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
