namespace Malayisha.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    private AuditLog(
        Guid id,
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        DateTime occurredAtUtc,
        string? metadataJson)
    {
        Id = id;
        ActorUserId = actorUserId;
        Action = DomainGuard.Required(action, nameof(action));
        TargetType = DomainGuard.Required(targetType, nameof(targetType));
        TargetId = targetId;
        OccurredAtUtc = occurredAtUtc;
        MetadataJson = metadataJson;
    }

    public Guid Id { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string TargetType { get; private set; } = string.Empty;
    public Guid TargetId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? MetadataJson { get; private set; }

    public static AuditLog Create(
        Guid id,
        Guid actorUserId,
        string action,
        string targetType,
        Guid targetId,
        DateTime nowUtc,
        string? metadataJson = null) =>
        new(id, actorUserId, action, targetType, targetId, nowUtc, metadataJson);
}
