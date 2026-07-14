using Malayisha.Domain.Entities;

namespace Malayisha.Application.Features.Profile;

internal static class ProfileMappings
{
    public static TransporterProfileResponse ToResponse(TransporterProfile profile) =>
        new(
            profile.Id,
            profile.UserId,
            profile.DisplayName,
            profile.RoutesServed,
            profile.VehicleDescription,
            profile.CapacityKg,
            profile.ProfilePhotoUrl,
            profile.IsVerified,
            profile.AverageRating,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc);

    public static PublicTransporterProfileResponse ToPublicResponse(TransporterProfile profile) =>
        new(
            profile.Id,
            profile.DisplayName,
            profile.RoutesServed,
            profile.VehicleDescription,
            profile.CapacityKg,
            profile.ProfilePhotoUrl,
            profile.IsVerified,
            profile.AverageRating);
}
