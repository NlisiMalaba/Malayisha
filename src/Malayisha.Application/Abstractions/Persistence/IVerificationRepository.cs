using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IVerificationRepository
{
    Task<Verification?> FindByIdAsync(Guid verificationId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveForProfileAsync(
        Guid transporterProfileId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Verification verification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Verification>> ListPendingOrderedBySubmittedAtAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
