using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository(MalayishaDbContext dbContext) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await dbContext.AuditLogs.AddAsync(auditLog, cancellationToken);
    }
}
