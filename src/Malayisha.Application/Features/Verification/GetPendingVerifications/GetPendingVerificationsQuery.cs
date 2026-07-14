using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Verification.GetPendingVerifications;

public sealed record GetPendingVerificationsQuery
    : IRequest<Result<IReadOnlyList<PendingVerificationResponse>>>;
