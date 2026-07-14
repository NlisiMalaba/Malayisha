using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Caching;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Trip.SearchTrips;
using Malayisha.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class TripSearchPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
    private const string CorridorOrigin = "Johannesburg";
    private const string CorridorDestination = "Harare";

    [Property(MaxTest = 100)]
    public bool Property19_SearchResults_SatisfyAllFiltersAndExclusions(
        int catalogSeed,
        bool useDateFilter,
        bool useMaxPrice,
        bool verifiedOnly,
        int pageSeed)
    {
        return RunFilterCorrectnessAsync(
            catalogSeed,
            useDateFilter,
            useMaxPrice,
            verifiedOnly,
            pageSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property20_SearchResults_RespectBoostThenVerifiedOrdering(int catalogSeed)
    {
        return RunOrderingInvariantAsync(catalogSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunFilterCorrectnessAsync(
        int catalogSeed,
        bool useDateFilter,
        bool useMaxPrice,
        bool verifiedOnly,
        int pageSeed)
    {
        var catalog = BuildCatalog(catalogSeed);
        var handler = CreateSearchHandler(catalog);

        DateOnly? departureFilter = useDateFilter
            ? DateOnly.FromDateTime(BaselineUtc.Date.AddDays((Math.Abs(catalogSeed) % 5) + 1))
            : null;

        decimal? maxPrice = useMaxPrice
            ? 100m + (Math.Abs(catalogSeed) % 500)
            : null;

        var page = (Math.Abs(pageSeed) % 3) + 1;
        var pageSize = 10;

        var result = await handler.Handle(
            new SearchTripsQuery(
                CorridorOrigin,
                CorridorDestination,
                departureFilter,
                maxPrice,
                verifiedOnly,
                page,
                pageSize),
            CancellationToken.None);

        if (result.IsError || result.Value is null)
        {
            return false;
        }

        var response = result.Value;
        var todayStart = DateTime.SpecifyKind(BaselineUtc.Date, DateTimeKind.Utc);

        foreach (var item in response.Items)
        {
            if (!CitiesEqual(item.OriginCity, CorridorOrigin)
                || !CitiesEqual(item.DestinationCity, CorridorDestination))
            {
                return false;
            }

            if (departureFilter is { } date
                && DateOnly.FromDateTime(item.DepartureDateUtc) != date)
            {
                return false;
            }

            if (maxPrice is { } price && item.PriceGuideZar > price)
            {
                return false;
            }

            if (verifiedOnly && !item.Transporter.IsVerified)
            {
                return false;
            }

            var source = catalog.Trips.Single(trip => trip.Id == item.Id);
            var profile = catalog.Profiles[source.TransporterProfileId];
            var user = catalog.Users[profile.UserId];

            if (source.IsDeleted
                || !user.IsActive
                || source.DepartureDateUtc < todayStart)
            {
                return false;
            }
        }

        var expectedVisible = catalog.Trips
            .Where(trip => MatchesFilters(trip, catalog, departureFilter, maxPrice, verifiedOnly, todayStart))
            .OrderByDescending(trip => trip.IsBoosted)
            .ThenByDescending(trip => catalog.Profiles[trip.TransporterProfileId].IsVerified)
            .ThenBy(trip => trip.DepartureDateUtc)
            .ThenBy(trip => trip.Id)
            .ToArray();

        if (response.TotalCount != expectedVisible.Length)
        {
            return false;
        }

        var expectedPage = expectedVisible
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(trip => trip.Id)
            .ToArray();

        return response.Items.Select(item => item.Id).SequenceEqual(expectedPage);
    }

    private static async Task<bool> RunOrderingInvariantAsync(int catalogSeed)
    {
        var catalog = BuildOrderingCatalog(catalogSeed);
        var handler = CreateSearchHandler(catalog);

        var result = await handler.Handle(
            new SearchTripsQuery(
                CorridorOrigin,
                CorridorDestination,
                DepartureDate: null,
                MaxPriceZar: null,
                VerifiedOnly: false,
                Page: 1,
                PageSize: 50),
            CancellationToken.None);

        if (result.IsError || result.Value is null)
        {
            return false;
        }

        var items = result.Value.Items;
        if (items.Count == 0)
        {
            return false;
        }

        var seenNonBoosted = false;
        foreach (var item in items)
        {
            if (!item.IsBoosted)
            {
                seenNonBoosted = true;
            }
            else if (seenNonBoosted)
            {
                return false;
            }
        }

        var seenUnverifiedAmongNonBoosted = false;
        foreach (var item in items.Where(i => !i.IsBoosted))
        {
            if (!item.Transporter.IsVerified)
            {
                seenUnverifiedAmongNonBoosted = true;
            }
            else if (seenUnverifiedAmongNonBoosted)
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesFilters(
        TripListing trip,
        Catalog catalog,
        DateOnly? departureFilter,
        decimal? maxPrice,
        bool verifiedOnly,
        DateTime todayStartUtc)
    {
        if (trip.IsDeleted
            || !CitiesEqual(trip.OriginCity, CorridorOrigin)
            || !CitiesEqual(trip.DestinationCity, CorridorDestination)
            || trip.DepartureDateUtc < todayStartUtc)
        {
            return false;
        }

        if (!catalog.Profiles.TryGetValue(trip.TransporterProfileId, out var profile)
            || !catalog.Users.TryGetValue(profile.UserId, out var user)
            || !user.IsActive)
        {
            return false;
        }

        if (departureFilter is { } date
            && DateOnly.FromDateTime(trip.DepartureDateUtc) != date)
        {
            return false;
        }

        if (maxPrice is { } price && trip.PriceGuideZar > price)
        {
            return false;
        }

        if (verifiedOnly && !profile.IsVerified)
        {
            return false;
        }

        return true;
    }

    private static bool CitiesEqual(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static SearchTripsQueryHandler CreateSearchHandler(Catalog catalog)
    {
        var repository = new InMemorySearchTripListingRepository(catalog);
        return new SearchTripsQueryHandler(
            repository,
            new NullCacheService(),
            new FixedTimeProvider(BaselineUtc),
            NullLogger<SearchTripsQueryHandler>.Instance);
    }

    private static Catalog BuildCatalog(int seed)
    {
        var users = new Dictionary<Guid, User>();
        var profiles = new Dictionary<Guid, TransporterProfile>();
        var trips = new List<TripListing>();

        var tripCount = (Math.Abs(seed) % 8) + 6;
        for (var index = 0; index < tripCount; index++)
        {
            var localSeed = seed + (index * 17);
            var userId = BuildGuid(localSeed, 1);
            var profileId = BuildGuid(localSeed, 2);
            var tripId = BuildGuid(localSeed, 3);

            var user = User.Create(userId, $"+2782{Math.Abs(localSeed) % 100000000:D8}", Domain.Enums.UserRole.Transporter, BaselineUtc);
            if (localSeed % 7 == 0)
            {
                user.Deactivate(BaselineUtc);
            }

            var profile = TransporterProfile.Create(
                profileId,
                userId,
                $"Driver-{Math.Abs(localSeed)}",
                ["JHB-Harare"],
                "Bakkie",
                500m + (Math.Abs(localSeed) % 200),
                BaselineUtc);

            if (localSeed % 2 == 0)
            {
                profile.MarkVerified(BaselineUtc);
            }

            users[userId] = user;
            profiles[profileId] = profile;

            var useCorridor = localSeed % 5 != 0;
            var origin = useCorridor ? CorridorOrigin : "Cape Town";
            var destination = useCorridor ? CorridorDestination : "Bulawayo";
            var daysOffset = (localSeed % 11) - 3; // includes past, today, future
            var departure = DateTime.SpecifyKind(BaselineUtc.Date.AddDays(daysOffset), DateTimeKind.Utc);
            var price = 50m + (Math.Abs(localSeed) % 600);

            var trip = TripListing.Create(
                tripId,
                profileId,
                origin,
                destination,
                departure,
                80m + (Math.Abs(localSeed) % 100),
                price,
                BaselineUtc);

            if (localSeed % 6 == 0)
            {
                trip.MarkDeleted(BaselineUtc);
            }

            if (localSeed % 3 == 0 && daysOffset > 0)
            {
                trip.ApplyBoost(BaselineUtc, BaselineUtc.AddDays(2), BaselineUtc);
            }

            trips.Add(trip);
        }

        return new Catalog(users, profiles, trips);
    }

    private static Catalog BuildOrderingCatalog(int seed)
    {
        var users = new Dictionary<Guid, User>();
        var profiles = new Dictionary<Guid, TransporterProfile>();
        var trips = new List<TripListing>();

        // Ensure a mix of all four boost/verified combinations.
        var combinations = new (bool Boosted, bool Verified)[]
        {
            (true, true),
            (true, false),
            (false, true),
            (false, false),
            (true, false),
            (false, true),
            (false, false),
            (true, true)
        };

        for (var index = 0; index < combinations.Length; index++)
        {
            var (boosted, verified) = combinations[index];
            var localSeed = seed + (index * 31);
            var userId = BuildGuid(localSeed, 11);
            var profileId = BuildGuid(localSeed, 12);
            var tripId = BuildGuid(localSeed, 13);

            var user = User.Create(userId, $"+2783{Math.Abs(localSeed) % 100000000:D8}", Domain.Enums.UserRole.Transporter, BaselineUtc);
            var profile = TransporterProfile.Create(
                profileId,
                userId,
                $"OrderDriver-{index}-{Math.Abs(localSeed)}",
                ["JHB-Harare"],
                "Truck",
                700m,
                BaselineUtc);

            if (verified)
            {
                profile.MarkVerified(BaselineUtc);
            }

            users[userId] = user;
            profiles[profileId] = profile;

            var departure = DateTime.SpecifyKind(
                BaselineUtc.Date.AddDays(1 + (Math.Abs(localSeed) % 10)),
                DateTimeKind.Utc);

            var trip = TripListing.Create(
                tripId,
                profileId,
                CorridorOrigin,
                CorridorDestination,
                departure,
                100m,
                200m + index,
                BaselineUtc);

            if (boosted)
            {
                trip.ApplyBoost(BaselineUtc, BaselineUtc.AddDays(3), BaselineUtc);
            }

            trips.Add(trip);
        }

        return new Catalog(users, profiles, trips);
    }

    private static Guid BuildGuid(int seed, int salt)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ (salt * 0x11111111));
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * (31 + salt));
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~(seed + salt));
        return new Guid(bytes);
    }

    private sealed record Catalog(
        IReadOnlyDictionary<Guid, User> Users,
        IReadOnlyDictionary<Guid, TransporterProfile> Profiles,
        IReadOnlyList<TripListing> Trips);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class NullCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class =>
            Task.FromResult<T?>(null);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
            where T : class =>
            Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class InMemorySearchTripListingRepository(Catalog catalog) : ITripListingRepository
    {
        public Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(catalog.Trips.FirstOrDefault(trip => trip.Id == tripListingId));

        public Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasBlockingBookingsAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<TripSearchPage> SearchAsync(
            TripSearchCriteria criteria,
            CancellationToken cancellationToken = default)
        {
            var candidates = catalog.Trips.Select(trip =>
            {
                catalog.Profiles.TryGetValue(trip.TransporterProfileId, out var profile);
                var ownerActive = profile is not null
                    && catalog.Users.TryGetValue(profile.UserId, out var user)
                    && user.IsActive;

                return new TripSearchCandidate(
                    trip.Id,
                    trip.OriginCity,
                    trip.DestinationCity,
                    trip.DepartureDateUtc,
                    trip.PriceGuideZar,
                    trip.IsBoosted,
                    trip.IsDeleted,
                    profile?.IsVerified ?? false,
                    ownerActive);
            });

            var queryResult = TripSearchQueryBuilder.Apply(candidates, criteria);
            var tripsById = catalog.Trips.ToDictionary(trip => trip.Id);

            var hits = queryResult.Items
                .Select(item =>
                {
                    var trip = tripsById[item.Id];
                    var profile = catalog.Profiles[trip.TransporterProfileId];
                    return new TripSearchHit(trip, profile);
                })
                .ToArray();

            return Task.FromResult(new TripSearchPage(hits, queryResult.TotalCount));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
