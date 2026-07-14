using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Profile.CreateProfile;

public sealed record CreateProfileCommand(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg,
    string? ProfilePhotoUrl = null) : IRequest<Result<TransporterProfileResponse>>;
