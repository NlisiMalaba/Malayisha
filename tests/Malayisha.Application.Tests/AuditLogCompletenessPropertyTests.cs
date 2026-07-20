using System.Text.Json;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Common;
using Malayisha.Application.Features.Commission;
using Malayisha.Application.Features.Commission.InvoiceCommission;
using Malayisha.Application.Features.Commission.MarkCommissionPaid;
using Malayisha.Application.Features.Review;
using Malayisha.Application.Features.Review.HideReview;
using Malayisha.Application.Features.Review.RestoreReview;
using Malayisha.Application.Features.Trip;
using Malayisha.Application.Features.Trip.ApplyBoost;
using Malayisha.Application.Features.Trip.RemoveBoost;
using Malayisha.Application.Features.Verification;
using Malayisha.Application.Features.Verification.ApproveVerification;
using Malayisha.Application.Features.Verification.RejectVerification;
using Malayisha.Application.Tests.Support;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class AuditLogCompletenessPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
    private const int AdminMutationCount = 8;

    [Property(MaxTest = 100)]
    public bool Property37_SuccessfulAdminMutation_AppendsExactlyOneCompleteAuditLog(
        int mutationSeed,
        int entitySeed,
        int adminSeed,
        int metadataSeed)
    {
        return RunSuccessfulAdminMutationAuditAsync(mutationSeed, entitySeed, adminSeed, metadataSeed)
            .GetAwaiter()
            .GetResult();
    }

    [Property(MaxTest = 50)]
    public bool Property37_FailedAdminMutation_DoesNotAppendAuditLog(
        int mutationSeed,
        int adminSeed)
    {
        return RunFailedAdminMutationDoesNotAuditAsync(mutationSeed, adminSeed)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<bool> RunSuccessfulAdminMutationAuditAsync(
        int mutationSeed,
        int entitySeed,
        int adminSeed,
        int metadataSeed)
    {
        var mutationIndex = Math.Abs(mutationSeed) % AdminMutationCount;
        var adminUserId = BuildUserId(adminSeed ^ 0x77777777);
        var auditLogs = new InMemoryAuditLogRepository();
        var clock = new MutableTimeProvider(BaselineUtc.AddHours(Math.Abs(entitySeed) % 100));
        var auditCountBefore = auditLogs.Items.Count;

        var scenario = await BuildSuccessfulScenarioAsync(
            mutationIndex,
            entitySeed,
            metadataSeed,
            adminUserId,
            clock);

        var result = await ExecuteAuditedMutationAsync(scenario, auditLogs, clock);
        if (!result.IsSuccess)
        {
            return false;
        }

        var newAuditEntries = auditLogs.Items.Skip(auditCountBefore).ToArray();
        if (newAuditEntries.Length != 1)
        {
            return false;
        }

        var audit = newAuditEntries[0];
        return audit.ActorUserId == adminUserId
               && audit.TargetId == scenario.ExpectedTargetId
               && audit.Action == scenario.ExpectedAction
               && audit.TargetType == scenario.ExpectedTargetType
               && audit.OccurredAtUtc == clock.UtcNow
               && MetadataMatches(scenario.ExpectedMetadata, audit.MetadataJson);
    }

    private static async Task<bool> RunFailedAdminMutationDoesNotAuditAsync(int mutationSeed, int adminSeed)
    {
        var mutationIndex = Math.Abs(mutationSeed) % AdminMutationCount;
        var adminUserId = BuildUserId(adminSeed ^ unchecked((int)0x88888888));
        var auditLogs = new InMemoryAuditLogRepository();
        var clock = new MutableTimeProvider(BaselineUtc);
        var missingEntityId = BuildGuid((int)(mutationSeed ^ adminSeed));

        var scenario = new AdminMutationScenario(
            mutationIndex,
            missingEntityId,
            adminUserId,
            VerificationAuditActions.Approved,
            VerificationAuditActions.TargetType,
            null,
            new MinimalVerificationRepository(),
            new MinimalTransporterProfileRepository(),
            new MinimalReviewRepository(),
            new MinimalCommissionRecordRepository(),
            new MinimalTripListingRepository(),
            mutationIndex switch
            {
                0 => new ApproveVerificationCommand(missingEntityId, adminUserId),
                1 => new RejectVerificationCommand(missingEntityId, adminUserId, "reason"),
                2 => new HideReviewCommand(missingEntityId, adminUserId),
                3 => new RestoreReviewCommand(missingEntityId, adminUserId),
                4 => new InvoiceCommissionCommand(missingEntityId, adminUserId),
                5 => new MarkCommissionPaidCommand(missingEntityId, adminUserId),
                6 => new ApplyBoostCommand(
                    missingEntityId,
                    adminUserId,
                    BaselineUtc,
                    BaselineUtc.AddDays(3)),
                7 => new RemoveBoostCommand(missingEntityId, adminUserId),
                _ => throw new ArgumentOutOfRangeException(nameof(mutationIndex))
            });

        var auditCountBefore = auditLogs.Items.Count;
        var result = await ExecuteAuditedMutationAsync(scenario, auditLogs, clock);

        return !result.IsSuccess && auditLogs.Items.Count == auditCountBefore;
    }

    private static async Task<AdminMutationScenario> BuildSuccessfulScenarioAsync(
        int mutationIndex,
        int entitySeed,
        int metadataSeed,
        Guid adminUserId,
        MutableTimeProvider clock)
    {
        var verifications = new MinimalVerificationRepository();
        var profiles = new MinimalTransporterProfileRepository();
        var reviews = new MinimalReviewRepository();
        var commissions = new MinimalCommissionRecordRepository();
        var trips = new MinimalTripListingRepository();

        switch (mutationIndex)
        {
            case 0:
            {
                var verificationId = BuildGuid(entitySeed);
                var profileId = BuildGuid(entitySeed ^ 0x1111);
                profiles.Seed(CreateProfile(profileId, entitySeed));
                await verifications.AddAsync(Verification.Create(verificationId, profileId, BaselineUtc));

                return new AdminMutationScenario(
                    mutationIndex,
                    verificationId,
                    adminUserId,
                    VerificationAuditActions.Approved,
                    VerificationAuditActions.TargetType,
                    null,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new ApproveVerificationCommand(verificationId, adminUserId));
            }
            case 1:
            {
                var verificationId = BuildGuid(entitySeed);
                var profileId = BuildGuid(entitySeed ^ 0x2222);
                profiles.Seed(CreateProfile(profileId, entitySeed));
                await verifications.AddAsync(Verification.Create(verificationId, profileId, BaselineUtc));

                var rejectionReason = metadataSeed % 2 == 0
                    ? null
                    : $"Reason-{Math.Abs(metadataSeed)}";
                var expectedMetadata = string.IsNullOrWhiteSpace(rejectionReason)
                    ? null
                    : JsonSerializer.Serialize(new { rejectionReason = rejectionReason.Trim() });

                return new AdminMutationScenario(
                    mutationIndex,
                    verificationId,
                    adminUserId,
                    VerificationAuditActions.Rejected,
                    VerificationAuditActions.TargetType,
                    expectedMetadata,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new RejectVerificationCommand(verificationId, adminUserId, rejectionReason));
            }
            case 2:
            {
                var reviewId = BuildGuid(entitySeed);
                var profileId = BuildGuid(entitySeed ^ 0x3333);
                profiles.Seed(CreateProfile(profileId, entitySeed));
                await reviews.AddAsync(Review.Create(
                    reviewId,
                    BuildGuid(entitySeed + 1),
                    BuildUserId(entitySeed + 2),
                    profileId,
                    (Math.Abs(metadataSeed) % 5) + 1,
                    null,
                    BaselineUtc));

                return new AdminMutationScenario(
                    mutationIndex,
                    reviewId,
                    adminUserId,
                    ReviewAuditActions.Hidden,
                    ReviewAuditActions.TargetType,
                    null,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new HideReviewCommand(reviewId, adminUserId));
            }
            case 3:
            {
                var reviewId = BuildGuid(entitySeed);
                var profileId = BuildGuid(entitySeed ^ 0x4444);
                profiles.Seed(CreateProfile(profileId, entitySeed));
                var review = Review.Create(
                    reviewId,
                    BuildGuid(entitySeed + 1),
                    BuildUserId(entitySeed + 2),
                    profileId,
                    4,
                    null,
                    BaselineUtc);
                review.SetVisibility(isHidden: true, BaselineUtc);
                await reviews.AddAsync(review);

                return new AdminMutationScenario(
                    mutationIndex,
                    reviewId,
                    adminUserId,
                    ReviewAuditActions.Restored,
                    ReviewAuditActions.TargetType,
                    null,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new RestoreReviewCommand(reviewId, adminUserId));
            }
            case 4:
            {
                var recordId = BuildGuid(entitySeed);
                await commissions.AddAsync(CommissionRecord.Create(
                    recordId,
                    BuildGuid(entitySeed + 10),
                    BuildUserId(entitySeed + 20),
                    500m + Math.Abs(metadataSeed) % 1000,
                    CommissionConstants.StandardCommissionRate,
                    BaselineUtc));

                return new AdminMutationScenario(
                    mutationIndex,
                    recordId,
                    adminUserId,
                    CommissionAuditActions.Invoiced,
                    CommissionAuditActions.TargetType,
                    null,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new InvoiceCommissionCommand(recordId, adminUserId));
            }
            case 5:
            {
                var recordId = BuildGuid(entitySeed);
                var record = CommissionRecord.Create(
                    recordId,
                    BuildGuid(entitySeed + 10),
                    BuildUserId(entitySeed + 20),
                    750m,
                    CommissionConstants.StandardCommissionRate,
                    BaselineUtc);
                record.MarkInvoiced(adminUserId, BaselineUtc);
                await commissions.AddAsync(record);

                return new AdminMutationScenario(
                    mutationIndex,
                    recordId,
                    adminUserId,
                    CommissionAuditActions.Paid,
                    CommissionAuditActions.TargetType,
                    null,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new MarkCommissionPaidCommand(recordId, adminUserId));
            }
            case 6:
            {
                var tripId = BuildGuid(entitySeed);
                var profileId = BuildGuid(entitySeed ^ 0x5555);
                trips.Seed(TripListing.Create(
                    tripId,
                    profileId,
                    "Johannesburg",
                    "Harare",
                    BaselineUtc.AddDays(5),
                    200m,
                    600m,
                    BaselineUtc));
                var boostStart = BaselineUtc;
                var boostEnd = boostStart.AddDays((Math.Abs(metadataSeed) % 14) + 1);

                return new AdminMutationScenario(
                    mutationIndex,
                    tripId,
                    adminUserId,
                    TripBoostAuditActions.Applied,
                    TripBoostAuditActions.TargetType,
                    JsonSerializer.Serialize(new
                    {
                        boostStartAtUtc = boostStart,
                        boostEndAtUtc = boostEnd
                    }),
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new ApplyBoostCommand(tripId, adminUserId, boostStart, boostEnd));
            }
            case 7:
            {
                var tripId = BuildGuid(entitySeed);
                var profileId = BuildGuid(entitySeed ^ 0x6666);
                var trip = TripListing.Create(
                    tripId,
                    profileId,
                    "Johannesburg",
                    "Harare",
                    BaselineUtc.AddDays(5),
                    200m,
                    600m,
                    BaselineUtc);
                trip.ApplyBoost(BaselineUtc, BaselineUtc.AddDays(3), BaselineUtc);
                trips.Seed(trip);

                return new AdminMutationScenario(
                    mutationIndex,
                    tripId,
                    adminUserId,
                    TripBoostAuditActions.Removed,
                    TripBoostAuditActions.TargetType,
                    null,
                    verifications,
                    profiles,
                    reviews,
                    commissions,
                    trips,
                    new RemoveBoostCommand(tripId, adminUserId));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(mutationIndex));
        }
    }

    private static async Task<Domain.Common.IResultResponse> ExecuteAuditedMutationAsync(
        AdminMutationScenario scenario,
        InMemoryAuditLogRepository auditLogs,
        MutableTimeProvider clock)
    {
        return scenario.MutationIndex switch
        {
            0 => await ExecuteAudited(
                (ApproveVerificationCommand)scenario.Command,
                new ApproveVerificationCommandHandler(
                    scenario.Verifications,
                    scenario.Profiles,
                    clock,
                    NullLogger<ApproveVerificationCommandHandler>.Instance),
                auditLogs,
                clock),
            1 => await ExecuteAudited(
                (RejectVerificationCommand)scenario.Command,
                new RejectVerificationCommandHandler(
                    scenario.Verifications,
                    scenario.Profiles,
                    clock,
                    NullLogger<RejectVerificationCommandHandler>.Instance),
                auditLogs,
                clock),
            2 => await ExecuteAudited(
                (HideReviewCommand)scenario.Command,
                new HideReviewCommandHandler(
                    scenario.Reviews,
                    scenario.Profiles,
                    clock,
                    NullLogger<HideReviewCommandHandler>.Instance),
                auditLogs,
                clock),
            3 => await ExecuteAudited(
                (RestoreReviewCommand)scenario.Command,
                new RestoreReviewCommandHandler(
                    scenario.Reviews,
                    scenario.Profiles,
                    clock,
                    NullLogger<RestoreReviewCommandHandler>.Instance),
                auditLogs,
                clock),
            4 => await ExecuteAudited(
                (InvoiceCommissionCommand)scenario.Command,
                new InvoiceCommissionCommandHandler(
                    scenario.Commissions,
                    clock,
                    NullLogger<InvoiceCommissionCommandHandler>.Instance),
                auditLogs,
                clock),
            5 => await ExecuteAudited(
                (MarkCommissionPaidCommand)scenario.Command,
                new MarkCommissionPaidCommandHandler(
                    scenario.Commissions,
                    clock,
                    NullLogger<MarkCommissionPaidCommandHandler>.Instance),
                auditLogs,
                clock),
            6 => await ExecuteAudited(
                (ApplyBoostCommand)scenario.Command,
                new ApplyBoostCommandHandler(
                    scenario.Trips,
                    clock,
                    NullLogger<ApplyBoostCommandHandler>.Instance),
                auditLogs,
                clock),
            7 => await ExecuteAudited(
                (RemoveBoostCommand)scenario.Command,
                new RemoveBoostCommandHandler(
                    scenario.Trips,
                    clock,
                    NullLogger<RemoveBoostCommandHandler>.Instance),
                auditLogs,
                clock),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario.MutationIndex))
        };
    }

    private static async Task<Domain.Common.IResultResponse> ExecuteAudited<TRequest, TResponse>(
        TRequest command,
        IRequestHandler<TRequest, TResponse> handler,
        InMemoryAuditLogRepository auditLogs,
        MutableTimeProvider clock)
        where TRequest : IRequest<TResponse>
    {
        var auditedHandler = new AuditedHandler<TRequest, TResponse>(handler, auditLogs, clock);
        var response = await auditedHandler.Handle(command, CancellationToken.None);
        return (Domain.Common.IResultResponse)response!;
    }

    private static TransporterProfile CreateProfile(Guid profileId, int entitySeed) =>
        TransporterProfile.Create(
            profileId,
            BuildUserId(entitySeed),
            "Transporter",
            ["JHB-HRE"],
            "Van",
            500m,
            BaselineUtc);

    private static bool MetadataMatches(string? expected, string? actual)
    {
        if (expected is null)
        {
            return actual is null;
        }

        if (actual is null)
        {
            return false;
        }

        using var expectedDoc = JsonDocument.Parse(expected);
        using var actualDoc = JsonDocument.Parse(actual);
        return JsonSerializer.Serialize(expectedDoc.RootElement)
               == JsonSerializer.Serialize(actualDoc.RootElement);
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

    private static Guid BuildGuid(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x13579BDF);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 17);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed ^ 0x2468ACE0);
        return new Guid(bytes);
    }

    private sealed record AdminMutationScenario(
        int MutationIndex,
        Guid ExpectedTargetId,
        Guid AdminUserId,
        string ExpectedAction,
        string ExpectedTargetType,
        string? ExpectedMetadata,
        MinimalVerificationRepository Verifications,
        MinimalTransporterProfileRepository Profiles,
        MinimalReviewRepository Reviews,
        MinimalCommissionRecordRepository Commissions,
        MinimalTripListingRepository Trips,
        object Command);

    private sealed class MutableTimeProvider : TimeProvider
    {
        public MutableTimeProvider(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; set; }

        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
    }

    private sealed class MinimalVerificationRepository : IVerificationRepository
    {
        private readonly Dictionary<Guid, Verification> _byId = [];

        public Task<Verification?> FindByIdAsync(Guid verificationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(verificationId, out var verification) ? verification : null);

        public Task<bool> HasActiveForProfileAsync(
            Guid transporterProfileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Verification verification, CancellationToken cancellationToken = default)
        {
            _byId[verification.Id] = verification;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Verification>> ListPendingOrderedBySubmittedAtAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Verification>>([]);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MinimalTransporterProfileRepository : ITransporterProfileRepository
    {
        private readonly Dictionary<Guid, TransporterProfile> _byId = [];

        public void Seed(TransporterProfile profile) => _byId[profile.Id] = profile;

        public Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(profileId, out var profile) ? profile : null);

        public Task<TransporterProfile?> FindByIdForUpdateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(profileId, cancellationToken);

        public Task<IReadOnlyDictionary<Guid, TransporterProfile>> FindByIdsAsync(
            IEnumerable<Guid> profileIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, TransporterProfile>>(new Dictionary<Guid, TransporterProfile>());

        public Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransporterProfile?>(null);

        public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(TransporterProfile profile, CancellationToken cancellationToken = default)
        {
            _byId[profile.Id] = profile;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MinimalReviewRepository : IReviewRepository
    {
        private readonly Dictionary<Guid, Review> _byId = [];

        public Task<bool> ExistsByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<int>> ListPublicRatingsByTransporterProfileIdAsync(
            Guid transporterProfileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>(
                _byId.Values
                    .Where(review => review.TransporterProfileId == transporterProfileId && !review.IsHidden)
                    .Select(review => review.Rating)
                    .ToArray());

        public Task<IReadOnlyList<Review>> ListPublicByTransporterProfileIdAsync(
            Guid transporterProfileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Review>>([]);

        public Task<Review?> FindByIdForUpdateAsync(Guid reviewId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(reviewId, out var review) ? review : null);

        public Task<IReadOnlyList<Review>> ListAllOrderedByCreatedAtDescAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Review>>([]);

        public Task AddAsync(Review review, CancellationToken cancellationToken = default)
        {
            _byId[review.Id] = review;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MinimalCommissionRecordRepository : ICommissionRecordRepository
    {
        private readonly List<CommissionRecord> _items = [];

        public Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<CommissionRecord?> FindByIdForUpdateAsync(
            Guid commissionRecordId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == commissionRecordId));

        public Task<IReadOnlyList<CommissionRecord>> ListByCriteriaAsync(
            CommissionReportCriteria criteria,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommissionRecord>>([]);

        public Task AddAsync(CommissionRecord commissionRecord, CancellationToken cancellationToken = default)
        {
            _items.Add(commissionRecord);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class MinimalTripListingRepository : ITripListingRepository
    {
        private readonly Dictionary<Guid, TripListing> _byId = [];

        public void Seed(TripListing trip) => _byId[trip.Id] = trip;

        public Task<TripListing?> FindByIdAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(tripListingId, out var trip) ? trip : null);

        public Task<TripListing?> FindByIdForUpdateAsync(
            Guid tripListingId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(tripListingId, cancellationToken);

        public Task<IReadOnlyList<TripListing>> ListExpiredBoostedForUpdateAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TripListing>>([]);

        public Task AddAsync(TripListing tripListing, CancellationToken cancellationToken = default)
        {
            _byId[tripListing.Id] = tripListing;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<TripSearchPage> SearchAsync(TripSearchCriteria criteria, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TripSearchPage([], 0));

        public Task<bool> HasBlockingBookingsAsync(Guid tripListingId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
