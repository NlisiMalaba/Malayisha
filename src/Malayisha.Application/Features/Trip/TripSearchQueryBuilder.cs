using Malayisha.Application.Abstractions.Persistence;

namespace Malayisha.Application.Features.Trip;

/// <summary>
/// Pure filter/order/pagination rules for trip search. Kept independent of EF so unit and property
/// tests can verify Requirements 5.1–5.6 without a database.
/// </summary>
internal static class TripSearchQueryBuilder
{
    public static TripSearchQueryResult Apply(
        IEnumerable<TripSearchCandidate> candidates,
        TripSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(criteria);

        var filtered = candidates
            .Where(candidate => Matches(candidate, criteria))
            .OrderByDescending(candidate => candidate.IsBoosted)
            .ThenByDescending(candidate => candidate.IsTransporterVerified)
            .ThenBy(candidate => candidate.DepartureDateUtc)
            .ThenBy(candidate => candidate.Id)
            .ToArray();

        var pageItems = filtered
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToArray();

        return new TripSearchQueryResult(pageItems, filtered.Length);
    }

    public static bool Matches(TripSearchCandidate candidate, TripSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(criteria);

        if (candidate.IsDeleted || !candidate.IsOwnerActive)
        {
            return false;
        }

        var originCity = criteria.OriginCity.Trim().ToLowerInvariant();
        var destinationCity = criteria.DestinationCity.Trim().ToLowerInvariant();
        var todayStartUtc = DateTime.SpecifyKind(criteria.NowUtc.Date, DateTimeKind.Utc);

        if (candidate.DepartureDateUtc < todayStartUtc)
        {
            return false;
        }

        if (candidate.OriginCity.Trim().ToLowerInvariant() != originCity
            || candidate.DestinationCity.Trim().ToLowerInvariant() != destinationCity)
        {
            return false;
        }

        if (criteria.DepartureDate is { } departureDate)
        {
            var dayStartUtc = DateTime.SpecifyKind(departureDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);
            if (candidate.DepartureDateUtc < dayStartUtc || candidate.DepartureDateUtc >= dayEndUtc)
            {
                return false;
            }
        }

        if (criteria.MaxPriceZar is { } maxPriceZar && candidate.PriceGuideZar > maxPriceZar)
        {
            return false;
        }

        if (criteria.VerifiedOnly && !candidate.IsTransporterVerified)
        {
            return false;
        }

        return true;
    }
}

internal sealed record TripSearchCandidate(
    Guid Id,
    string OriginCity,
    string DestinationCity,
    DateTime DepartureDateUtc,
    decimal PriceGuideZar,
    bool IsBoosted,
    bool IsDeleted,
    bool IsTransporterVerified,
    bool IsOwnerActive);

internal sealed record TripSearchQueryResult(
    IReadOnlyList<TripSearchCandidate> Items,
    int TotalCount);
