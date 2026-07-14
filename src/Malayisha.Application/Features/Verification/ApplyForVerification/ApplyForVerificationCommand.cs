using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Verification.ApplyForVerification;

public sealed record ApplyForVerificationCommand(Guid UserId)
    : IRequest<Result<VerificationResponse>>;
