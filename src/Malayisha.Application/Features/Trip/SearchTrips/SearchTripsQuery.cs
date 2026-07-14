using Malayisha.Application.Common;
using MediatR;

namespace Malayisha.Application.Features.Trip.SearchTrips;

public sealed record SearchTripsQuery(
    string OriginCity,
    string DestinationCity,
    DateOnly? DepartureDate = null,
    decimal? MaxPriceZar = null,
    bool VerifiedOnly = false,
    int Page = TripValidation.DefaultPage,
    int PageSize = TripValidation.DefaultPageSize) : IRequest<Result<TripSearchPageResponse>>;
