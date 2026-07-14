using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Profile.UpdateProfile;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> RoutesServed,
    string VehicleDescription,
    decimal CapacityKg) : IRequest<Result<TransporterProfileResponse>>;
