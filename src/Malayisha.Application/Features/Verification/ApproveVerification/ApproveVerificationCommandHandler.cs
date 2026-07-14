using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Verification.ApproveVerification;

internal sealed class ApproveVerificationCommandHandler(
    IVerificationRepository verificationRepository,
    ITransporterProfileRepository profileRepository,
    TimeProvider timeProvider,
    ILogger<ApproveVerificationCommandHandler> logger)
    : IRequestHandler<ApproveVerificationCommand, Result<VerificationResponse>>
{
    public async Task<Result<VerificationResponse>> Handle(
        ApproveVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var verification = await verificationRepository.FindByIdAsync(request.VerificationId, cancellationToken);
        if (verification is null)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.VerificationNotFound);
        }

        var profile = await profileRepository.FindByIdForUpdateAsync(
            verification.TransporterProfileId,
            cancellationToken);

        if (profile is null)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.ProfileNotFound);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var approveResult = verification.Approve(request.AdminUserId, nowUtc);
        if (approveResult.IsError)
        {
            return Result<VerificationResponse>.Error(VerificationErrorCodes.InvalidVerificationStatus);
        }

        profile.MarkVerified(nowUtc);

        // Single SaveChanges across tracked verification + profile for atomic commit.
        await verificationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Verification {VerificationId} approved by admin {AdminUserId} for profile {ProfileId}",
            verification.Id,
            request.AdminUserId,
            profile.Id);

        return Result<VerificationResponse>.Success(VerificationMappings.ToResponse(verification));
    }
}
