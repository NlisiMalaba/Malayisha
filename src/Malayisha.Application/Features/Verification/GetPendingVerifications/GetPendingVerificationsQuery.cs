using Malayisha.Application.Common;
using Malayisha.Application.Common.Authorization;
using Malayisha.Domain.Enums;
using MediatR;

namespace Malayisha.Application.Features.Verification.GetPendingVerifications;

[AuthorizeRoles(UserRole.Admin)]
public sealed record GetPendingVerificationsQuery
    : IRequest<Result<IReadOnlyList<PendingVerificationResponse>>>;
