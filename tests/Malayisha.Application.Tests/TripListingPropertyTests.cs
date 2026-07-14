using System.Globalization;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Trip.CreateTrip;
using Malayisha.Application.Features.Trip.DeleteTrip;
using Malayisha.Application.Features.Trip.GetShareLink;
using Malayisha.Application.Features.Trip.UpdateTrip;
using Malayisha.Application.Options;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class TripListingPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
    private const string DeepLinkBaseUrl = "https://app.omalayisha.com/trips";

    [Property(MaxTest = 100)]
    public bool Property14_Create_RoundTripsWritableFields(
        int ownerSeed,
        int originSeed,
        int destinationSeed,
        int daysAhead,
        int capacitySeed,
        int priceSeed,
        int descriptionSeed)
    {
        return RunCreateRoundTripAsync(
            ownerSeed,
            originSeed,
            destinationSeed,
            daysAhead,
            capacitySeed,
            priceSeed,
            descriptionSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property15_NonFutureDeparture_IsRejected_AndFutureSucceeds(
        int ownerSeed,
        int daysOffset)
    {
        return RunFutureDateConstraintAsync(ownerSeed, daysOffset).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property16_NonOwner_CannotUpdateOrDelete(
        int ownerSeed,
        int otherSeed,
        int payloadSeed)
    {
        return RunOwnershipEnforcementAsync(ownerSeed, otherSeed, payloadSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property17_BlockingBookingStatuses_PreventDelete(int ownerSeed, int statusSeed)
    {
        return RunActiveBookingBlocksDeleteAsync(ownerSeed, statusSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property18_ShareLink_ContainsRequiredFields(
        int ownerSeed,
        int payloadSeed,
        bool isVerified)
    {
        return RunShareLinkContainsRequiredFieldsAsync(ownerSeed, payloadSeed, isVerified)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<bool> RunCreateRoundTripAsync(
        int ownerSeed,
        int originSeed,
        int destinationSeed,
        int daysAhead,
        int capacitySeed,
        int priceSeed,
        int descriptionSeed)
    {
        var harness = Harness.Create(ownerSeed);
        var payload = BuildValidTripPayload(
            originSeed,
            destinationSeed,
            NormalizeFutureDays(daysAhead),
            capacitySeed,
            priceSeed,
            descriptionSeed);

        var create = await harness.CreateHandler.Handle(
            new CreateTripCommand(
                harness.OwnerUserId,
                payload.OriginCity,
                payload.DestinationCity,
                payload.DepartureDateUtc,
                payload.AvailableCapacityKg,
                payload.PriceGuideZar,
                payload.Description),
            CancellationToken.None);

        if (create.IsError || create.Value is null || create.Value.Id == Guid.Empty)
        {
            return false;
        }

        var created = create.Value;
        var stored = await harness.Trips.FindByIdAsync(created.Id);

        return stored is not null
               && harness.Trips.TotalCount == 1
               && FieldsMatch(created, payload)
               && FieldsMatch(
                   TripMappings.ToResponse(stored),
                   payload);
    }

    private static async Task<bool> RunFutureDateConstraintAsync(int ownerSeed, int daysOffset)
    {
        var harness = Harness.Create(ownerSeed);
        var clampedOffset = daysOffset == int.MinValue
            ? 0
            : (Math.Abs(daysOffset) % 400) * Math.Sign(daysOffset);

        var departure = DateTime.SpecifyKind(BaselineUtc.Date.AddDays(clampedOffset), DateTimeKind.Utc);
        var payload = BuildValidTripPayload(1, 2, 1, 10, 20, 0) with
        {
            DepartureDateUtc = departure
        };

        var create = await harness.CreateHandler.Handle(
            new CreateTripCommand(
                harness.OwnerUserId,
                payload.OriginCity,
                payload.DestinationCity,
                payload.DepartureDateUtc,
                payload.AvailableCapacityKg,
                payload.PriceGuideZar,
                payload.Description),
            CancellationToken.None);

        if (clampedOffset <= 0)
        {
            return create.IsError
                   && create.ErrorCode == TripErrorCodes.DepartureDateMustBeFuture
                   && create.Value is null
                   && harness.Trips.TotalCount == 0;
        }

        return create.IsSuccess
               && create.Value is not null
               && harness.Trips.TotalCount == 1
               && create.Value.DepartureDateUtc == departure;
    }

    private static async Task<bool> RunOwnershipEnforcementAsync(
        int ownerSeed,
        int otherSeed,
        int payloadSeed)
    {
        if (ownerSeed == otherSeed)
        {
            otherSeed ^= 0x55AA55AA;
        }

        var harness = Harness.Create(ownerSeed);
        var otherUserId = BuildUserId(otherSeed);
        var otherProfile = TransporterProfile.Create(
            Guid.NewGuid(),
            otherUserId,
            "Other Transporter",
            ["Pretoria-Bulawayo"],
            "Panel van",
            500m,
            BaselineUtc);
        harness.Profiles.Seed(otherProfile);

        var createPayload = BuildValidTripPayload(payloadSeed, payloadSeed + 3, 5, payloadSeed + 7, payloadSeed + 11, payloadSeed + 13);
        var create = await harness.CreateHandler.Handle(
            new CreateTripCommand(
                harness.OwnerUserId,
                createPayload.OriginCity,
                createPayload.DestinationCity,
                createPayload.DepartureDateUtc,
                createPayload.AvailableCapacityKg,
                createPayload.PriceGuideZar,
                createPayload.Description),
            CancellationToken.None);

        if (create.IsError || create.Value is null)
        {
            return false;
        }

        var tripId = create.Value.Id;
        var updatePayload = BuildValidTripPayload(
            payloadSeed + 17,
            payloadSeed + 19,
            9,
            payloadSeed + 23,
            payloadSeed + 29,
            payloadSeed + 31);

        var update = await harness.UpdateHandler.Handle(
            new UpdateTripCommand(
                otherUserId,
                tripId,
                updatePayload.OriginCity,
                updatePayload.DestinationCity,
                updatePayload.DepartureDateUtc,
                updatePayload.AvailableCapacityKg,
                updatePayload.PriceGuideZar,
                updatePayload.Description),
            CancellationToken.None);

        var delete = await harness.DeleteHandler.Handle(
            new DeleteTripCommand(otherUserId, tripId),
            CancellationToken.None);

        var stored = await harness.Trips.FindByIdAsync(tripId);

        return update.IsError
               && update.ErrorCode == TripErrorCodes.NotTripOwner
               && delete.IsError
               && delete.ErrorCode == TripErrorCodes.NotTripOwner
               && stored is not null
               && !stored.IsDeleted
               && FieldsMatch(TripMappings.ToResponse(stored), createPayload);
    }

    private static async Task<bool> RunActiveBookingBlocksDeleteAsync(int ownerSeed, int statusSeed)
    {
        var blockingStatuses = new[]
        {
            BookingStatus.Confirmed,
            BookingStatus.InTransit,
            BookingStatus.Delivered
        };

        var status = blockingStatuses[Math.Abs(statusSeed) % blockingStatuses.Length];
        var harness = Harness.Create(ownerSeed);

        var create = await harness.CreateHandler.Handle(
            new CreateTripCommand(
                harness.OwnerUserId,
                "Johannesburg",
                "Harare",
                BaselineUtc.Date.AddDays(10),
                100m,
                250m,
                "Fragile"),
            CancellationToken.None);

        if (create.IsError || create.Value is null)
        {
            return false;
        }

        harness.Trips.SetBlockingBookings(create.Value.Id, hasBlocking: true, status);

        var delete = await harness.DeleteHandler.Handle(
            new DeleteTripCommand(harness.OwnerUserId, create.Value.Id),
            CancellationToken.None);

        var stored = await harness.Trips.FindByIdAsync(create.Value.Id);

        return delete.IsError
               && delete.ErrorCode == TripErrorCodes.ActiveBookingsBlockDelete
               && stored is not null
               && !stored.IsDeleted;
    }

    private static async Task<bool> RunShareLinkContainsRequiredFieldsAsync(
        int ownerSeed,
        int payloadSeed,
        bool isVerified)
    {
        var harness = Harness.Create(ownerSeed);
        if (isVerified)
        {
            var ownerProfile = await harness.Profiles.FindByIdForUpdateAsync(harness.OwnerProfileId);
            ownerProfile!.MarkVerified(BaselineUtc);
            await harness.Profiles.SaveChangesAsync();
        }

        var payload = BuildValidTripPayload(
            payloadSeed,
            payloadSeed + 5,
            NormalizeFutureDays(payloadSeed),
            payloadSeed + 9,
            payloadSeed + 11,
            payloadSeed + 13);

        var create = await harness.CreateHandler.Handle(
            new CreateTripCommand(
                harness.OwnerUserId,
                payload.OriginCity,
                payload.DestinationCity,
                payload.DepartureDateUtc,
                payload.AvailableCapacityKg,
                payload.PriceGuideZar,
                payload.Description),
            CancellationToken.None);

        if (create.IsError || create.Value is null)
        {
            return false;
        }

        var share = await harness.ShareLinkHandler.Handle(
            new GetShareLinkQuery(create.Value.Id),
            CancellationToken.None);

        if (share.IsError || share.Value is null)
        {
            return false;
        }

        var url = share.Value.Url;
        if (!url.StartsWith("https://wa.me/?text=", StringComparison.Ordinal))
        {
            return false;
        }

        var encoded = url["https://wa.me/?text=".Length..];
        var message = Uri.UnescapeDataString(encoded);

        var expectedDate = DateOnly.FromDateTime(payload.DepartureDateUtc)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var expectedCapacity = payload.AvailableCapacityKg.ToString("0.##", CultureInfo.InvariantCulture);
        var expectedPrice = payload.PriceGuideZar.ToString("0.##", CultureInfo.InvariantCulture);
        var deepLink = $"{DeepLinkBaseUrl}/{create.Value.Id:D}";

        var transporter = await harness.Profiles.FindByIdAsync(harness.OwnerProfileId);

        return transporter is not null
               && message.Contains(transporter.DisplayName, StringComparison.Ordinal)
               && message.Contains(payload.OriginCity.Trim(), StringComparison.Ordinal)
               && message.Contains(payload.DestinationCity.Trim(), StringComparison.Ordinal)
               && message.Contains(expectedDate, StringComparison.Ordinal)
               && message.Contains(expectedCapacity, StringComparison.Ordinal)
               && message.Contains(expectedPrice, StringComparison.Ordinal)
               && message.Contains(deepLink, StringComparison.Ordinal)
               && Uri.TryCreate(deepLink, UriKind.Absolute, out var deepLinkUri)
               && deepLinkUri.Scheme is "https" or "http";
    }

    private static bool FieldsMatch(TripListingResponse actual, TripPayload expected) =>
        actual.OriginCity == expected.OriginCity.Trim()
        && actual.DestinationCity == expected.DestinationCity.Trim()
        && actual.DepartureDateUtc == expected.DepartureDateUtc
        && actual.AvailableCapacityKg == expected.AvailableCapacityKg
        && actual.PriceGuideZar == expected.PriceGuideZar
        && actual.Description == NormalizeDescription(expected.Description);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static int NormalizeFutureDays(int daysAhead)
    {
        var days = (Math.Abs(daysAhead) % 365) + 1;
        return days;
    }

    private static TripPayload BuildValidTripPayload(
        int originSeed,
        int destinationSeed,
        int daysAhead,
        int capacitySeed,
        int priceSeed,
        int descriptionSeed)
    {
        var origin = BuildToken("Origin", originSeed, TripValidation.CityMaxLength);
        var destination = BuildToken("Dest", destinationSeed, TripValidation.CityMaxLength);
        var departure = DateTime.SpecifyKind(BaselineUtc.Date.AddDays(NormalizeFutureDays(daysAhead)), DateTimeKind.Utc);
        var capacity = (Math.Abs(capacitySeed) % 5_000) + 1 + ((Math.Abs(capacitySeed) % 100) / 100m);
        var price = (Math.Abs(priceSeed) % 10_000) + 1 + ((Math.Abs(priceSeed) % 100) / 100m);
        var description = descriptionSeed % 3 == 0
            ? null
            : BuildToken("Desc", descriptionSeed, TripValidation.DescriptionMaxLength);

        return new TripPayload(origin, destination, departure, capacity, price, description);
    }

    private static string BuildToken(string prefix, int seed, int maxLength)
    {
        var suffix = Math.Abs(seed).ToString(CultureInfo.InvariantCulture);
        var token = $"{prefix}-{suffix}";
        return token.Length <= maxLength ? token : token[..maxLength];
    }

    private static Guid BuildUserId(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private sealed record TripPayload(
        string OriginCity,
        string DestinationCity,
        DateTime DepartureDateUtc,
        decimal AvailableCapacityKg,
        decimal PriceGuideZar,
        string? Description);

    private sealed class Harness
    {
        private Harness(
            Guid ownerUserId,
            Guid ownerProfileId,
            InMemoryTransporterProfileRepository profiles,
            InMemoryTripListingRepository trips,
            CreateTripCommandHandler createHandler,
            UpdateTripCommandHandler updateHandler,
            DeleteTripCommandHandler deleteHandler,
            GetShareLinkQueryHandler shareLinkHandler)
        {
            OwnerUserId = ownerUserId;
            OwnerProfileId = ownerProfileId;
            Profiles = profiles;
            Trips = trips;
            CreateHandler = createHandler;
            UpdateHandler = updateHandler;
            DeleteHandler = deleteHandler;
            ShareLinkHandler = shareLinkHandler;
        }

        public Guid OwnerUserId { get; }
        public Guid OwnerProfileId { get; }
        public InMemoryTransporterProfileRepository Profiles { get; }
        public InMemoryTripListingRepository Trips { get; }
        public CreateTripCommandHandler CreateHandler { get; }
        public UpdateTripCommandHandler UpdateHandler { get; }
        public DeleteTripCommandHandler DeleteHandler { get; }
        public GetShareLinkQueryHandler ShareLinkHandler { get; }

        public static Harness Create(int ownerSeed)
        {
            var ownerUserId = BuildUserId(ownerSeed);
            var ownerProfileId = Guid.NewGuid();
            var profile = TransporterProfile.Create(
                ownerProfileId,
                ownerUserId,
                "Owner Transporter",
                ["JHB-Harare"],
                "1-ton bakkie",
                800m,
                BaselineUtc);

            var profiles = new InMemoryTransporterProfileRepository();
            profiles.Seed(profile);

            var trips = new InMemoryTripListingRepository();
            var clock = new FixedTimeProvider(BaselineUtc);
            var appLinks = Microsoft.Extensions.Options.Options.Create(
                new AppLinkOptions { TripDeepLinkBaseUrl = DeepLinkBaseUrl });

            return new Harness(
                ownerUserId,
                ownerProfileId,
                profiles,
                trips,
                new CreateTripCommandHandler(
                    profiles,
                    trips,
                    clock,
                    NullLogger<CreateTripCommandHandler>.Instance),
                new UpdateTripCommandHandler(
                    profiles,
                    trips,
                    clock,
                    NullLogger<UpdateTripCommandHandler>.Instance),
                new DeleteTripCommandHandler(
                    profiles,
                    trips,
                    clock,
                    NullLogger<DeleteTripCommandHandler>.Instance),
                new GetShareLinkQueryHandler(
                    trips,
                    profiles,
                    appLinks,
                    NullLogger<GetShareLinkQueryHandler>.Instance));
        }
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InMemoryTripListingRepository : ITripListingRepository
    {
        private readonly Dictionary<Guid, TripListing> _byId = [];
        private readonly HashSet<Guid> _blockingTripIds = [];

        public int TotalCount => _byId.Count;

        public void SetBlockingBookings(Guid tripListingId, bool hasBlocking, BookingStatus _)
        {
            if (hasBlocking)
            {
                _blockingTripIds.Add(tripListingId);
            }
            else
            {
                _blockingTripIds.Remove(tripListingId);
            }
        }

        public Task<TripListing?> FindByIdAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(tripListingId, out var trip) ? trip : null);

        public Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default)
        {
            _byId[tripListing.Id] = tripListing;
            return Task.CompletedTask;
        }

        public Task<bool> HasBlockingBookingsAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_blockingTripIds.Contains(tripListingId));

        public Task<TripSearchPage> SearchAsync(
            TripSearchCriteria criteria,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Search is covered by dedicated search property tests.");

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryTransporterProfileRepository : ITransporterProfileRepository
    {
        private readonly Dictionary<Guid, TransporterProfile> _byId = [];
        private readonly Dictionary<Guid, Guid> _idByUserId = [];

        public void Seed(TransporterProfile profile)
        {
            _byId[profile.Id] = profile;
            _idByUserId[profile.UserId] = profile.Id;
        }

        public Task<TransporterProfile?> FindByIdAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(profileId, out var profile) ? profile : null);

        public Task<TransporterProfile?> FindByIdForUpdateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(profileId, cancellationToken);

        public Task<IReadOnlyDictionary<Guid, TransporterProfile>> FindByIdsAsync(
            IEnumerable<Guid> profileIds,
            CancellationToken cancellationToken = default)
        {
            var profiles = profileIds
                .Distinct()
                .Where(id => _byId.ContainsKey(id))
                .ToDictionary(id => id, id => _byId[id]);

            return Task.FromResult<IReadOnlyDictionary<Guid, TransporterProfile>>(profiles);
        }

        public Task<TransporterProfile?> FindByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (!_idByUserId.TryGetValue(userId, out var profileId))
            {
                return Task.FromResult<TransporterProfile?>(null);
            }

            return Task.FromResult<TransporterProfile?>(_byId[profileId]);
        }

        public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_idByUserId.ContainsKey(userId));

        public Task AddAsync(TransporterProfile profile, CancellationToken cancellationToken = default)
        {
            Seed(profile);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
