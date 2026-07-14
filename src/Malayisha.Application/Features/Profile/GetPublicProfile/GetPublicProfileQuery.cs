using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Profile.GetPublicProfile;

public sealed record GetPublicProfileQuery(Guid ProfileId)
    : IRequest<Result<PublicTransporterProfileResponse>>;
