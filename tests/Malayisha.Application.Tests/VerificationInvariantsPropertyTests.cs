using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Verification;
using Malayisha.Application.Features.Verification.ApplyForVerification;
using Malayisha.Application.Features.Verification.ApproveVerification;
using Malayisha.Application.Features.Verification.GetPendingVerifications;
using Malayisha.Application.Features.Verification.RejectVerification;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class VerificationInvariantsPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool Property9_Apply_CreatesExactlyOnePendingVerification(int userSeed)
    {
        return RunApplyCreatesPendingAsync(userSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property10_SecondApply_RejectedWhenPendingExists(int userSeed)
    {
        return RunSecondApplyRejectedWhenActiveAsync(userSeed, leaveApproved: false).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property10_SecondApply_RejectedWhenApprovedExists(int userSeed)
    {
        return RunSecondApplyRejectedWhenActiveAsync(userSeed, leaveApproved: true).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property11_Approve_AtomicallyUpdatesVerificationProfileAndAudit(int userSeed, int adminSeed)
    {
        return RunApproveAtomicAsync(userSeed, adminSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property12_Reject_UpdatesState_AndLeavesIsVerifiedFalse(
        int userSeed,
        int adminSeed,
        int reasonSeed)
    {
        return RunRejectAsync(userSeed, adminSeed, reasonSeed).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property13_PendingList_IsSortedBySubmittedAtAscending(int[] timeOffsets)
    {
        return RunPendingSortAsync(timeOffsets).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunApplyCreatesPendingAsync(int userSeed)
    {
        var harness = Harness.Create(userSeed);
        var result = await harness.ApplyHandler.Handle(
            new ApplyForVerificationCommand(harness.UserId),
            CancellationToken.None);

        if (result.IsError || result.Value is null)
        {
            return false;
        }

        var created = result.Value;
        var stored = await harness.Verifications.FindByIdAsync(created.Id);

        return stored is not null
               && stored.Status == VerificationStatus.Pending
               && stored.TransporterProfileId == harness.ProfileId
               && harness.Verifications.CountForProfile(harness.ProfileId) == 1
               && harness.Verifications.TotalCount == 1;
    }

    private static async Task<bool> RunSecondApplyRejectedWhenActiveAsync(int userSeed, bool leaveApproved)
    {
        var harness = Harness.Create(userSeed);
        var first = await harness.ApplyHandler.Handle(
            new ApplyForVerificationCommand(harness.UserId),
            CancellationToken.None);

        if (first.IsError || first.Value is null)
        {
            return false;
        }

        if (leaveApproved)
        {
            var approve = await harness.ApproveHandler.Handle(
                new ApproveVerificationCommand(first.Value.Id, BuildUserId(userSeed ^ 0xABCDEF)),
                CancellationToken.None);

            if (approve.IsError)
            {
                return false;
            }
        }

        var second = await harness.ApplyHandler.Handle(
            new ApplyForVerificationCommand(harness.UserId),
            CancellationToken.None);

        return second.IsError
               && second.ErrorCode == VerificationErrorCodes.ActiveVerificationExists
               && second.Value is null
               && harness.Verifications.CountForProfile(harness.ProfileId) == 1
               && harness.Verifications.TotalCount == 1;
    }

    private static async Task<bool> RunApproveAtomicAsync(int userSeed, int adminSeed)
    {
        var harness = Harness.Create(userSeed);
        var adminUserId = BuildUserId(adminSeed ^ 0x11111111);

        var apply = await harness.ApplyHandler.Handle(
            new ApplyForVerificationCommand(harness.UserId),
            CancellationToken.None);

        if (apply.IsError || apply.Value is null)
        {
            return false;
        }

        harness.Verifications.ResetSaveChangesCount();
        harness.Clock.UtcNow = BaselineUtc.AddMinutes(15);

        var approve = await harness.ApproveHandler.Handle(
            new ApproveVerificationCommand(apply.Value.Id, adminUserId),
            CancellationToken.None);

        if (approve.IsError || approve.Value is null)
        {
            return false;
        }

        var verification = await harness.Verifications.FindByIdAsync(apply.Value.Id);
        var profile = await harness.Profiles.FindByIdAsync(harness.ProfileId);
        var audit = harness.AuditLogs.SingleOrDefault(
            log => log.TargetId == apply.Value.Id
                   && log.Action == VerificationAuditActions.Approved);

        return verification is not null
               && profile is not null
               && verification.Status == VerificationStatus.Approved
               && verification.ReviewedByAdminUserId == adminUserId
               && verification.ReviewedAtUtc == harness.Clock.UtcNow
               && profile.IsVerified
               && profile.UpdatedAtUtc == harness.Clock.UtcNow
               && audit is not null
               && audit.ActorUserId == adminUserId
               && audit.TargetType == VerificationAuditActions.TargetType
               && audit.OccurredAtUtc == harness.Clock.UtcNow
               && harness.Verifications.SaveChangesCount == 1;
    }

    private static async Task<bool> RunRejectAsync(int userSeed, int adminSeed, int reasonSeed)
    {
        var harness = Harness.Create(userSeed);
        var adminUserId = BuildUserId(adminSeed ^ 0x22222222);
        var reason = reasonSeed % 3 == 0
            ? null
            : $"Incomplete docs-{Math.Abs(reasonSeed)}";

        var apply = await harness.ApplyHandler.Handle(
            new ApplyForVerificationCommand(harness.UserId),
            CancellationToken.None);

        if (apply.IsError || apply.Value is null)
        {
            return false;
        }

        harness.Clock.UtcNow = BaselineUtc.AddMinutes(20);

        var reject = await harness.RejectHandler.Handle(
            new RejectVerificationCommand(apply.Value.Id, adminUserId, reason),
            CancellationToken.None);

        if (reject.IsError || reject.Value is null)
        {
            return false;
        }

        var verification = await harness.Verifications.FindByIdAsync(apply.Value.Id);
        var profile = await harness.Profiles.FindByIdAsync(harness.ProfileId);
        var expectedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var audit = harness.AuditLogs.SingleOrDefault(
            log => log.TargetId == apply.Value.Id
                   && log.Action == VerificationAuditActions.Rejected);

        return verification is not null
               && profile is not null
               && verification.Status == VerificationStatus.Rejected
               && verification.ReviewedByAdminUserId == adminUserId
               && verification.ReviewedAtUtc == harness.Clock.UtcNow
               && verification.RejectionReason == expectedReason
               && !profile.IsVerified
               && audit is not null
               && audit.ActorUserId == adminUserId
               && audit.OccurredAtUtc == harness.Clock.UtcNow;
    }

    private static async Task<bool> RunPendingSortAsync(int[] timeOffsets)
    {
        if (timeOffsets is null || timeOffsets.Length == 0)
        {
            return true;
        }

        // Keep the suite bounded while still covering arbitrary distinct orderings.
        var distinctOffsets = timeOffsets
            .Select(offset => Math.Abs(offset) % 10_000)
            .Distinct()
            .Take(12)
            .ToArray();

        if (distinctOffsets.Length < 2)
        {
            return true;
        }

        var clock = new MutableTimeProvider(BaselineUtc);
        var profiles = new InMemoryTransporterProfileRepository();
        var verifications = new InMemoryVerificationRepository();
        var auditLogs = new InMemoryAuditLogRepository();

        var applyHandler = new ApplyForVerificationCommandHandler(
            profiles,
            verifications,
            clock,
            NullLogger<ApplyForVerificationCommandHandler>.Instance);

        var getPendingHandler = new GetPendingVerificationsQueryHandler(verifications, profiles);

        var createdIds = new List<Guid>(distinctOffsets.Length);

        for (var index = 0; index < distinctOffsets.Length; index++)
        {
            var userId = BuildUserId(index + 1000);
            var profile = TransporterProfile.Create(
                Guid.NewGuid(),
                userId,
                $"Transporter-{index}",
                [$"Route-{index}"],
                $"Vehicle-{index}",
                500m + index,
                BaselineUtc);

            await profiles.AddAsync(profile);
            await profiles.SaveChangesAsync();

            clock.UtcNow = BaselineUtc.AddMinutes(distinctOffsets[index]);
            var apply = await applyHandler.Handle(
                new ApplyForVerificationCommand(userId),
                CancellationToken.None);

            if (apply.IsError || apply.Value is null)
            {
                return false;
            }

            createdIds.Add(apply.Value.Id);
        }

        var pendingResult = await getPendingHandler.Handle(
            new GetPendingVerificationsQuery(),
            CancellationToken.None);

        if (pendingResult.IsError || pendingResult.Value is null)
        {
            return false;
        }

        var pending = pendingResult.Value;
        if (pending.Count != distinctOffsets.Length)
        {
            return false;
        }

        for (var index = 1; index < pending.Count; index++)
        {
            if (pending[index].SubmittedAtUtc < pending[index - 1].SubmittedAtUtc)
            {
                return false;
            }
        }

        var expectedOrder = createdIds
            .Select(id => verifications.GetRequired(id))
            .OrderBy(verification => verification.SubmittedAtUtc)
            .Select(verification => verification.Id)
            .ToArray();

        return pending.Select(item => item.Id).SequenceEqual(expectedOrder);
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

    private sealed class Harness
    {
        private Harness(
            Guid userId,
            Guid profileId,
            InMemoryTransporterProfileRepository profiles,
            InMemoryVerificationRepository verifications,
            InMemoryAuditLogRepository auditLogs,
            MutableTimeProvider clock,
            ApplyForVerificationCommandHandler applyHandler,
            ApproveVerificationCommandHandler approveHandler,
            RejectVerificationCommandHandler rejectHandler)
        {
            UserId = userId;
            ProfileId = profileId;
            Profiles = profiles;
            Verifications = verifications;
            AuditLogs = auditLogs.Items;
            Clock = clock;
            ApplyHandler = applyHandler;
            ApproveHandler = approveHandler;
            RejectHandler = rejectHandler;
        }

        public Guid UserId { get; }
        public Guid ProfileId { get; }
        public InMemoryTransporterProfileRepository Profiles { get; }
        public InMemoryVerificationRepository Verifications { get; }
        public IReadOnlyList<AuditLog> AuditLogs { get; }
        public MutableTimeProvider Clock { get; }
        public ApplyForVerificationCommandHandler ApplyHandler { get; }
        public ApproveVerificationCommandHandler ApproveHandler { get; }
        public RejectVerificationCommandHandler RejectHandler { get; }

        public static Harness Create(int userSeed)
        {
            var userId = BuildUserId(userSeed);
            var profileId = Guid.NewGuid();
            var profile = TransporterProfile.Create(
                profileId,
                userId,
                "Test Transporter",
                ["JHB-Harare"],
                "1-ton bakkie",
                800m,
                BaselineUtc);

            var profiles = new InMemoryTransporterProfileRepository();
            profiles.Seed(profile);

            var verifications = new InMemoryVerificationRepository();
            var auditLogs = new InMemoryAuditLogRepository();
            var clock = new MutableTimeProvider(BaselineUtc);

            return new Harness(
                userId,
                profileId,
                profiles,
                verifications,
                auditLogs,
                clock,
                new ApplyForVerificationCommandHandler(
                    profiles,
                    verifications,
                    clock,
                    NullLogger<ApplyForVerificationCommandHandler>.Instance),
                new ApproveVerificationCommandHandler(
                    verifications,
                    profiles,
                    auditLogs,
                    clock,
                    NullLogger<ApproveVerificationCommandHandler>.Instance),
                new RejectVerificationCommandHandler(
                    verifications,
                    profiles,
                    auditLogs,
                    clock,
                    NullLogger<RejectVerificationCommandHandler>.Instance));
        }
    }

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;

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

    private sealed class InMemoryVerificationRepository : IVerificationRepository
    {
        private readonly Dictionary<Guid, Verification> _byId = [];

        public int TotalCount => _byId.Count;

        public int SaveChangesCount { get; private set; }

        public void ResetSaveChangesCount() => SaveChangesCount = 0;

        public int CountForProfile(Guid profileId) =>
            _byId.Values.Count(verification => verification.TransporterProfileId == profileId);

        public Verification GetRequired(Guid verificationId) => _byId[verificationId];

        public Task<Verification?> FindByIdAsync(
            Guid verificationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_byId.TryGetValue(verificationId, out var verification) ? verification : null);

        public Task<bool> HasActiveForProfileAsync(
            Guid transporterProfileId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _byId.Values.Any(verification =>
                    verification.TransporterProfileId == transporterProfileId
                    && verification.IsActive));

        public Task AddAsync(Verification verification, CancellationToken cancellationToken = default)
        {
            _byId[verification.Id] = verification;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Verification>> ListPendingOrderedBySubmittedAtAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Verification>>(
                _byId.Values
                    .Where(verification => verification.Status == VerificationStatus.Pending)
                    .OrderBy(verification => verification.SubmittedAtUtc)
                    .ToArray());

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
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
