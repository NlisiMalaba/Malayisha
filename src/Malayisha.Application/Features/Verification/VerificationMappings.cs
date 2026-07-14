using Malayisha.Domain.Entities;

namespace Malayisha.Application.Features.Verification;

internal static class VerificationMappings
{
    public static VerificationResponse ToResponse(Domain.Entities.Verification verification) =>
        new(
            verification.Id,
            verification.TransporterProfileId,
            verification.Status,
            verification.SubmittedAtUtc,
            verification.ReviewedByAdminUserId,
            verification.ReviewedAtUtc,
            verification.RejectionReason);

    public static PendingVerificationResponse ToPendingResponse(
        Domain.Entities.Verification verification,
        TransporterProfile profile) =>
        new(
            verification.Id,
            verification.SubmittedAtUtc,
            new PendingVerificationProfileResponse(
                profile.Id,
                profile.DisplayName,
                profile.RoutesServed,
                profile.VehicleDescription,
                profile.CapacityKg,
                profile.ProfilePhotoUrl,
                profile.IsVerified,
                profile.AverageRating));
}
