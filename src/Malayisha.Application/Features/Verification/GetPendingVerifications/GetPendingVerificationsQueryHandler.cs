using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Verification.GetPendingVerifications;

internal sealed class GetPendingVerificationsQueryHandler(
    IVerificationRepository verificationRepository,
    ITransporterProfileRepository profileRepository)
    : IRequestHandler<GetPendingVerificationsQuery, Result<IReadOnlyList<PendingVerificationResponse>>>
{
    public async Task<Result<IReadOnlyList<PendingVerificationResponse>>> Handle(
        GetPendingVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await verificationRepository.ListPendingOrderedBySubmittedAtAsync(cancellationToken);
        var profilesById = await profileRepository.FindByIdsAsync(
            pending.Select(verification => verification.TransporterProfileId),
            cancellationToken);

        var response = new List<PendingVerificationResponse>(pending.Count);
        foreach (var verification in pending)
        {
            if (!profilesById.TryGetValue(verification.TransporterProfileId, out var profile))
            {
                continue;
            }

            response.Add(VerificationMappings.ToPendingResponse(verification, profile));
        }

        return Result<IReadOnlyList<PendingVerificationResponse>>.Success(response);
    }
}
