namespace Malayisha.Application.Common.Authorization;

public interface IAuditableAdminCommand
{
    Guid AdminUserId { get; }

    Guid TargetId { get; }

    string AuditAction { get; }

    string TargetType { get; }

    string? MetadataJson => null;
}
