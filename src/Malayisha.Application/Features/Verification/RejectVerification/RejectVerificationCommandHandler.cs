using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Verification.RejectVerification;

internal sealed class RejectVerificationCommandHandler(
    IVerificationRepository verificationRepository,
    ITransporterProfileRepository profileRepository,
    TimeProvider timeProvider,
    ILogger<RejectVerificationCommandHandler> logger)
    : IRequestHandler<RejectVerificationCommand, Result<VerificationResponse>>
{
    public async Task<Result<VerificationResponse>> Handle(
        RejectVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var verification = await verificationRepository.FindByIdAsync(request.VerificationId, cancellationToken);
        if (verification is null)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.VerificationNotFound);
        }

        var profile = await profileRepository.FindByIdAsync(
            verification.TransporterProfileId,
            cancellationToken);

        if (profile is null)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.ProfileNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var rejectResult = verification.Reject(request.AdminUserId, request.RejectionReason, nowUtc);
        if (rejectResult.IsError)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.InvalidVerificationStatus);
        }

        await verificationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Verification {VerificationId} rejected by admin {AdminUserId} for profile {ProfileId}",
            verification.Id,
            request.AdminUserId,
            verification.TransporterProfileId);

        return Result<VerificationResponse>.Success(VerificationMappings.ToResponse(verification));
    }
}
