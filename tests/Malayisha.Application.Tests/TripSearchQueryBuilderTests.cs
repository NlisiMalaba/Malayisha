using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Trip;

namespace Malayisha.Application.Tests;

public sealed class TripSearchQueryBuilderTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
    private const string Origin = "Johannesburg";
    private const string Destination = "Harare";

    private static readonly Guid MatchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static IEnumerable<object[]> OptionalFilterCombinations()
    {
        foreach (var useDate in new[] { false, true })
        foreach (var useMaxPrice in new[] { false, true })
        foreach (var verifiedOnly in new[] { false, true })
        {
            yield return new object[] { useDate, useMaxPrice, verifiedOnly };
        }
    }

    [Theory]
    [MemberData(nameof(OptionalFilterCombinations))]
    public void Apply_OptionalFilterCombinations_ReturnOnlyCandidatesMatchingAllActiveFilters(
        bool useDate,
        bool useMaxPrice,
        bool verifiedOnly)
    {
        var targetDate = DateOnly.FromDateTime(NowUtc.Date.AddDays(3));
        var maxPrice = 300m;

        var candidates = new[]
        {
            Candidate(
                MatchId,
                departureDays: 3,
                price: 250m,
                verified: true,
                boosted: false),
            Candidate(
                OtherId,
                departureDays: 5,
                price: 400m,
                verified: false,
                boosted: false),
            Candidate(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                departureDays: 3,
                price: 200m,
                verified: false,
                boosted: true),
            Candidate(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                origin: "Cape Town",
                destination: "Bulawayo",
                departureDays: 3,
                price: 100m,
                verified: true)
        };

        var criteria = Criteria(
            departureDate: useDate ? targetDate : null,
            maxPriceZar: useMaxPrice ? maxPrice : null,
            verifiedOnly: verifiedOnly);

        var result = TripSearchQueryBuilder.Apply(candidates, criteria);

        Assert.All(result.Items, item =>
        {
            Assert.Equal(Origin, item.OriginCity, ignoreCase: true);
            Assert.Equal(Destination, item.DestinationCity, ignoreCase: true);

            if (useDate)
            {
                Assert.Equal(targetDate, DateOnly.FromDateTime(item.DepartureDateUtc));
            }

            if (useMaxPrice)
            {
                Assert.True(item.PriceGuideZar <= maxPrice);
            }

            if (verifiedOnly)
            {
                Assert.True(item.IsTransporterVerified);
            }
        });

        var expectedIds = candidates
            .Where(candidate => TripSearchQueryBuilder.Matches(candidate, criteria))
            .OrderByDescending(candidate => candidate.IsBoosted)
            .ThenByDescending(candidate => candidate.IsTransporterVerified)
            .ThenBy(candidate => candidate.DepartureDateUtc)
            .ThenBy(candidate => candidate.Id)
            .Select(candidate => candidate.Id)
            .ToArray();

        Assert.Equal(expectedIds.Length, result.TotalCount);
        Assert.Equal(expectedIds, result.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Apply_ExcludesDeletedListings()
    {
        var candidates = new[]
        {
            Candidate(MatchId, departureDays: 2, price: 100m, verified: true),
            Candidate(OtherId, departureDays: 2, price: 100m, verified: true, isDeleted: true)
        };

        var result = TripSearchQueryBuilder.Apply(candidates, Criteria());

        Assert.Equal(new[] { MatchId }, result.Items.Select(item => item.Id).ToArray());
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void Apply_ExcludesPastDepartureListings_ButIncludesTodayAndFuture()
    {
        var yesterday = Candidate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            departureDays: -1,
            price: 100m,
            verified: true);
        var today = Candidate(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            departureDays: 0,
            price: 100m,
            verified: true);
        var tomorrow = Candidate(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            departureDays: 1,
            price: 100m,
            verified: true);

        var result = TripSearchQueryBuilder.Apply([yesterday, today, tomorrow], Criteria());

        Assert.Equal(
            new[] { today.Id, tomorrow.Id },
            result.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Apply_ExcludesInactiveOwnerProfiles()
    {
        var active = Candidate(MatchId, departureDays: 2, price: 100m, verified: true);
        var inactive = Candidate(OtherId, departureDays: 2, price: 100m, verified: true, isOwnerActive: false);

        var result = TripSearchQueryBuilder.Apply([active, inactive], Criteria());

        Assert.Equal(new[] { MatchId }, result.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Apply_OrdersBoostedBeforeNonBoosted_ThenVerifiedBeforeUnverified()
    {
        var boostedUnverified = Candidate(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            departureDays: 4,
            price: 100m,
            verified: false,
            boosted: true);
        var boostedVerified = Candidate(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            departureDays: 5,
            price: 100m,
            verified: true,
            boosted: true);
        var plainVerified = Candidate(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            departureDays: 2,
            price: 100m,
            verified: true,
            boosted: false);
        var plainUnverified = Candidate(
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            departureDays: 1,
            price: 100m,
            verified: false,
            boosted: false);

        var result = TripSearchQueryBuilder.Apply(
            [plainUnverified, plainVerified, boostedUnverified, boostedVerified],
            Criteria());

        Assert.Equal(
            new[]
            {
                boostedVerified.Id,
                boostedUnverified.Id,
                plainVerified.Id,
                plainUnverified.Id
            },
            result.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Apply_PaginatesAfterFilteringAndOrdering()
    {
        var candidates = Enumerable.Range(0, 5)
            .Select(index => Candidate(
                Guid.Parse($"eeeeeeee-eeee-eeee-eeee-{index:D12}"),
                departureDays: index + 1,
                price: 100m + index,
                verified: index % 2 == 0,
                boosted: index == 4))
            .ToArray();

        var result = TripSearchQueryBuilder.Apply(candidates, Criteria(page: 2, pageSize: 2));

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);

        var fullOrder = TripSearchQueryBuilder.Apply(candidates, Criteria(page: 1, pageSize: 50))
            .Items
            .Select(item => item.Id)
            .ToArray();

        Assert.Equal(fullOrder.Skip(2).Take(2).ToArray(), result.Items.Select(item => item.Id).ToArray());
    }

    [Fact]
    public void Matches_IsCaseInsensitiveForCorridorCities()
    {
        var candidate = Candidate(
            MatchId,
            origin: " johannesburg ",
            destination: "HARARE",
            departureDays: 2,
            price: 150m,
            verified: true);

        Assert.True(TripSearchQueryBuilder.Matches(candidate, Criteria(origin: "Johannesburg", destination: "Harare")));
    }

    private static TripSearchCriteria Criteria(
        string origin = Origin,
        string destination = Destination,
        DateOnly? departureDate = null,
        decimal? maxPriceZar = null,
        bool verifiedOnly = false,
        int page = 1,
        int pageSize = 50) =>
        new(origin, destination, departureDate, maxPriceZar, verifiedOnly, NowUtc, page, pageSize);

    private static TripSearchCandidate Candidate(
        Guid id,
        int departureDays,
        decimal price,
        bool verified,
        bool boosted = false,
        bool isDeleted = false,
        bool isOwnerActive = true,
        string origin = Origin,
        string destination = Destination) =>
        new(
            id,
            origin,
            destination,
            DateTime.SpecifyKind(NowUtc.Date.AddDays(departureDays), DateTimeKind.Utc),
            price,
            boosted,
            isDeleted,
            verified,
            isOwnerActive);
}
