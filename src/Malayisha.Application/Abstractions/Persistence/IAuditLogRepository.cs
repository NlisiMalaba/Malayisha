using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
