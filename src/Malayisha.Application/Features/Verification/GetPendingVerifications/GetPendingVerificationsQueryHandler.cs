using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Verification.GetPendingVerifications;

internal sealed class GetPendingVerificationsQueryHandler(IVerificationRepository verificationRepository)
    : IRequestHandler<GetPendingVerificationsQuery, Result<IReadOnlyList<PendingVerificationResponse>>>
{
    public async Task<Result<IReadOnlyList<PendingVerificationResponse>>> Handle(
        GetPendingVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await verificationRepository.ListPendingOrderedBySubmittedAtAsync(cancellationToken);
        IReadOnlyList<PendingVerificationResponse> response = pending
            .Select(VerificationMappings.ToPendingResponse)
            .ToArray();

        return Result<IReadOnlyList<PendingVerificationResponse>>.Success(response);
    }
}
