using System.Globalization;
using Malayisha.Application.Abstractions.Caching;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Malayisha.Application.Features.Trip.SearchTrips;

internal sealed class SearchTripsQueryHandler(
    ITripListingRepository tripListingRepository,
    ICacheService cacheService,
    TimeProvider timeProvider,
    ILogger<SearchTripsQueryHandler> logger)
    : IRequestHandler<SearchTripsQuery, Result<TripSearchPageResponse>>
{
    public async Task<Result<TripSearchPageResponse>> Handle(
        SearchTripsQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildCacheKey(request);
        var redisKey = RedisKeys.TripSearch(cacheKey);

        var cached = await cacheService.GetAsync<TripSearchPageResponse>(redisKey, cancellationToken);
        if (cached is not null)
        {
            logger.LogDebug("Trip search cache hit for key {CacheKey}", cacheKey);
            return Result<TripSearchPageResponse>.Success(cached);
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var page = await tripListingRepository.SearchAsync(
            new TripSearchCriteria(
                request.OriginCity,
                request.DestinationCity,
                request.DepartureDate,
                request.MaxPriceZar,
                request.VerifiedOnly,
                nowUtc,
                request.Page,
                request.PageSize),
            cancellationToken);

        var response = TripMappings.ToSearchPage(page, request.Page, request.PageSize);

        await cacheService.SetAsync(redisKey, response, TripValidation.SearchCacheTtl, cancellationToken);

        logger.LogInformation(
            "Trip search for {OriginCity} → {DestinationCity} returned {Count} of {TotalCount} results",
            request.OriginCity,
            request.DestinationCity,
            response.Items.Count,
            response.TotalCount);

        return Result<TripSearchPageResponse>.Success(response);
    }

    private static string BuildCacheKey(SearchTripsQuery request)
    {
        var origin = request.OriginCity.Trim().ToLowerInvariant();
        var destination = request.DestinationCity.Trim().ToLowerInvariant();
        var date = request.DepartureDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";
        var maxPrice = request.MaxPriceZar?.ToString("0.##", CultureInfo.InvariantCulture) ?? "-";
        var verified = request.VerifiedOnly ? "1" : "0";

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{origin}|{destination}|{date}|{maxPrice}|{verified}|{request.Page}|{request.PageSize}");
    }
}
