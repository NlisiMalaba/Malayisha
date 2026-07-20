using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Trip.ApplyBoost;
using Malayisha.Application.Features.Trip.RemoveBoost;
using Malayisha.Domain.Entities;
using Malayisha.Infrastructure.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class BoostPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool Property33_BoostLifecycleCorrectness_IdempotentExpiry(
        int tripSeed,
        int durationSeed,
        int adminSeed)
    {
        return RunBoostLifecycleAsync(tripSeed, durationSeed, adminSeed)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<bool> RunBoostLifecycleAsync(
        int tripSeed,
        int durationSeed,
        int adminSeed)
    {
        var harness = BoostTestHarness.Create(tripSeed);
        var adminUserId = BuildUserId(adminSeed ^ 0x44444444);
        var boostStart = BaselineUtc;
        var boostEnd = boostStart.AddDays((Math.Abs(durationSeed) % 14) + 1);

        var apply = await harness.ApplyHandler.Handle(
            new ApplyBoostCommand(harness.TripId, adminUserId, boostStart, boostEnd),
            CancellationToken.None);

        if (apply.IsError
            || apply.Value is null
            || !apply.Value.IsBoosted
            || apply.Value.BoostStartAtUtc != boostStart
            || apply.Value.BoostEndAtUtc != boostEnd)
        {
            return false;
        }

        var applyAudit = harness.AuditLogs.SingleOrDefault(
            log => log.TargetId == harness.TripId
                   && log.Action == TripBoostAuditActions.Applied);

        if (applyAudit is null || applyAudit.ActorUserId != adminUserId)
        {
            return false;
        }

        harness.Clock.UtcNow = boostEnd.AddMinutes(-1);
        await harness.ExpireJob.ExecuteAsync(CancellationToken.None);

        var tripBeforeExpiry = await harness.Repository.FindByIdAsync(harness.TripId);
        if (tripBeforeExpiry is null
            || !tripBeforeExpiry.IsBoosted
            || tripBeforeExpiry.BoostEndAtUtc != boostEnd)
        {
            return false;
        }

        harness.Clock.UtcNow = boostEnd;
        await harness.ExpireJob.ExecuteAsync(CancellationToken.None);

        var tripAfterExpiry = await harness.Repository.FindByIdAsync(harness.TripId);
        if (tripAfterExpiry is null
            || tripAfterExpiry.IsBoosted
            || tripAfterExpiry.BoostStartAtUtc is not null
            || tripAfterExpiry.BoostEndAtUtc is not null)
        {
            return false;
        }

        await harness.ExpireJob.ExecuteAsync(CancellationToken.None);

        tripAfterExpiry = await harness.Repository.FindByIdAsync(harness.TripId);
        if (tripAfterExpiry is null || tripAfterExpiry.IsBoosted)
        {
            return false;
        }

        var secondTripId = BuildGuid(tripSeed ^ unchecked((int)0x99999999));
        var secondTrip = CreateTrip(secondTripId, harness.ProfileId, BaselineUtc);
        await harness.Repository.AddAsync(secondTrip);

        var secondApply = await harness.ApplyHandler.Handle(
            new ApplyBoostCommand(secondTripId, adminUserId, boostStart, boostEnd),
            CancellationToken.None);

        if (secondApply.IsError || secondApply.Value is null || !secondApply.Value.IsBoosted)
        {
            return false;
        }

        var remove = await harness.RemoveHandler.Handle(
            new RemoveBoostCommand(secondTripId, adminUserId),
            CancellationToken.None);

        if (remove.IsError || remove.Value is null || remove.Value.IsBoosted)
        {
            return false;
        }

        var removeAudit = harness.AuditLogs.SingleOrDefault(
            log => log.TargetId == secondTripId
                   && log.Action == TripBoostAuditActions.Removed);

        if (removeAudit is null || removeAudit.ActorUserId != adminUserId)
        {
            return false;
        }

        harness.Clock.UtcNow = boostEnd.AddDays(1);
        await harness.ExpireJob.ExecuteAsync(CancellationToken.None);

        var secondTripFinal = await harness.Repository.FindByIdAsync(secondTripId);
        return secondTripFinal is not null && !secondTripFinal.IsBoosted;
    }

    private static TripListing CreateTrip(Guid tripId, Guid profileId, DateTime createdAtUtc) =>
        TripListing.Create(
            tripId,
            profileId,
            "Johannesburg",
            "Harare",
            createdAtUtc.AddDays(5),
            200m,
            600m,
            createdAtUtc);

    private static Guid BuildGuid(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed * 7);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed ^ 0x01020304);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
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

    private sealed class BoostTestHarness
    {
        private BoostTestHarness(
            Guid tripId,
            Guid profileId,
            MutableTimeProvider clock,
            InMemoryTripListingRepository repository,
            InMemoryAuditLogRepository auditLogs,
            ApplyBoostCommandHandler applyHandler,
            RemoveBoostCommandHandler removeHandler,
            ExpireBoostsJob expireJob)
        {
            TripId = tripId;
            ProfileId = profileId;
            Clock = clock;
            Repository = repository;
            AuditLogs = auditLogs.Items;
            ApplyHandler = applyHandler;
            RemoveHandler = removeHandler;
            ExpireJob = expireJob;
        }

        public Guid TripId { get; }
        public Guid ProfileId { get; }
        public MutableTimeProvider Clock { get; }
        public InMemoryTripListingRepository Repository { get; }
        public IReadOnlyList<AuditLog> AuditLogs { get; }
        public ApplyBoostCommandHandler ApplyHandler { get; }
        public RemoveBoostCommandHandler RemoveHandler { get; }
        public ExpireBoostsJob ExpireJob { get; }

        public static BoostTestHarness Create(int tripSeed)
        {
            var tripId = BuildGuid(tripSeed);
            var profileId = BuildGuid(tripSeed ^ 0x2468ACE0);
            var clock = new MutableTimeProvider(BaselineUtc);
            var repository = new InMemoryTripListingRepository();
            var auditLogs = new InMemoryAuditLogRepository();

            var trip = CreateTrip(tripId, profileId, BaselineUtc);
            repository.Seed(trip);

            return new BoostTestHarness(
                tripId,
                profileId,
                clock,
                repository,
                auditLogs,
                new ApplyBoostCommandHandler(
                    repository,
                    auditLogs,
                    clock,
                    NullLogger<ApplyBoostCommandHandler>.Instance),
                new RemoveBoostCommandHandler(
                    repository,
                    auditLogs,
                    clock,
                    NullLogger<RemoveBoostCommandHandler>.Instance),
                new ExpireBoostsJob(
                    repository,
                    clock,
                    NullLogger<ExpireBoostsJob>.Instance));
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public MutableTimeProvider(DateTime utcNow) =>
            UtcNow = utcNow;

        public DateTime UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }

    private sealed class InMemoryAuditLogRepository : IAuditLogRepository
    {
        private readonly List<AuditLog> _items = [];

        public IReadOnlyList<AuditLog> Items => _items;

        public Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            _items.Add(auditLog);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryTripListingRepository : ITripListingRepository
    {
        private readonly Dictionary<Guid, TripListing> _byId = [];

        public void Seed(TripListing trip) => _byId[trip.Id] = trip;

        public Task<TripListing?> FindByIdAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(tripListingId, out var trip) ? trip : null);

        public Task<TripListing?> FindByIdForUpdateAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(tripListingId, cancellationToken);

        public Task<IReadOnlyList<TripListing>> ListExpiredBoostedForUpdateAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var items = _byId.Values
                .Where(trip =>
                    trip.IsBoosted
                    && trip.BoostEndAtUtc != null
                    && trip.BoostEndAtUtc <= nowUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<TripListing>>(items);
        }

        public Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default)
        {
            _byId[tripListing.Id] = tripListing;
            return Task.CompletedTask;
        }

        public Task<bool> HasBlockingBookingsAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<TripSearchPage> SearchAsync(
            TripSearchCriteria criteria,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
