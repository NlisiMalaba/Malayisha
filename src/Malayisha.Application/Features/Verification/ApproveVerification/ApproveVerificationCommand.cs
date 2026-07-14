using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Verification.ApproveVerification;

public sealed record ApproveVerificationCommand(Guid VerificationId, Guid AdminUserId)
    : IRequest<Result<VerificationResponse>>;
