using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Verification.RejectVerification;

public sealed record RejectVerificationCommand(
    Guid VerificationId,
    Guid AdminUserId,
    string? RejectionReason = null) : IRequest<Result<VerificationResponse>>;
