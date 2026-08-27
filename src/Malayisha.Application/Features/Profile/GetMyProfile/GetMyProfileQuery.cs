using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Profile.GetMyProfile;

public sealed record GetMyProfileQuery(Guid UserId) : IRequest<Result<TransporterProfileResponse>>;
