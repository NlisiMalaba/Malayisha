using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Verification.RejectVerification;

internal sealed class RejectVerificationCommandHandler(
    IVerificationRepository verificationRepository,
    ITransporterProfileRepository profileRepository,
    IAuditLogRepository auditLogRepository,
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

        await auditLogRepository.AddAsync(
            AuditLog.Create(
                Guid.NewGuid(),
                request.AdminUserId,
                VerificationAuditActions.Rejected,
                VerificationAuditActions.TargetType,
                verification.Id,
                nowUtc,
                string.IsNullOrWhiteSpace(request.RejectionReason)
                    ? null
                    : System.Text.Json.JsonSerializer.Serialize(new { rejectionReason = request.RejectionReason })),
            cancellationToken);

        await verificationRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Verification {VerificationId} rejected by admin {AdminUserId} for profile {ProfileId}",
            verification.Id,
            request.AdminUserId,
            verification.TransporterProfileId);

        return Result<VerificationResponse>.Success(VerificationMappings.ToResponse(verification));
    }
}
