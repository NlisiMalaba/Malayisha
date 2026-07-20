using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Verification.ApplyForVerification;

[AuthorizeRoles(UserRole.Transporter)]
public sealed record ApplyForVerificationCommand(Guid UserId)
    : IRequest<Result<VerificationResponse>>;
