using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Verification.ApproveVerification;

[AuthorizeRoles(UserRole.Admin)]
public sealed record ApproveVerificationCommand(Guid VerificationId, Guid AdminUserId)
    : IRequest<Result<VerificationResponse>>, IAuditableAdminCommand
{
    public Guid TargetId => VerificationId;

    public string AuditAction => Verification.VerificationAuditActions.Approved;

    public string TargetType => Verification.VerificationAuditActions.TargetType;
}
