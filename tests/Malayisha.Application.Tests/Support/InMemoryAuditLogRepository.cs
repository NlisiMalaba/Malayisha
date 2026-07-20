using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;

namespace Malayisha.Application.Tests.Support;

internal sealed class InMemoryAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLog> _items = [];

    public IReadOnlyList<AuditLog> Items => _items;

    public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _items.Add(auditLog);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
