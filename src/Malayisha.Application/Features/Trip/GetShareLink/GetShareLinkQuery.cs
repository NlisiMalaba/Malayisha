using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Trip.GetShareLink;

public sealed record GetShareLinkQuery(Guid TripListingId) : IRequest<Result<ShareLinkResponse>>;

public sealed record ShareLinkResponse(string Url);
