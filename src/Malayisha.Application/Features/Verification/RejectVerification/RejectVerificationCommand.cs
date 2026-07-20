using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Verification.RejectVerification;

[AuthorizeRoles(UserRole.Admin)]
public sealed record RejectVerificationCommand(
    Guid VerificationId,
    Guid AdminUserId,
    string? RejectionReason = null) : IRequest<Result<VerificationResponse>>, IAuditableAdminCommand
{
    public Guid TargetId => VerificationId;

    public string AuditAction => Verification.VerificationAuditActions.Rejected;

    public string TargetType => Verification.VerificationAuditActions.TargetType;

    public string? MetadataJson =>
        string.IsNullOrWhiteSpace(RejectionReason)
            ? null
            : System.Text.Json.JsonSerializer.Serialize(new { rejectionReason = RejectionReason.Trim() });
}
