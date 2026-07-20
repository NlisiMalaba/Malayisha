using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Common;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Jobs;
using Malayisha.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class NotificationPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool Property38_FailingPush_RetriesWithNonDecreasingBackoffThenDiscards(
        int userSeed,
        int tokenSeed,
        int titleSeed)
    {
        return RunFailingPushRetryScenarioAsync(userSeed, tokenSeed, titleSeed)
            .GetAwaiter()
            .GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property39_MarketingPush_RequiresOptIn(
        int userSeed,
        bool marketingOptIn,
        bool sendMarketing)
    {
        return RunMarketingOptInScenarioAsync(userSeed, marketingOptIn, sendMarketing)
            .GetAwaiter()
            .GetResult();
    }

    private static async Task<bool> RunFailingPushRetryScenarioAsync(
        int userSeed,
        int tokenSeed,
        int titleSeed)
    {
        var userId = BuildUserId(userSeed);
        var deviceToken = BuildDeviceToken(tokenSeed);
        var title = $"Title-{Math.Abs(titleSeed) % 1000}";
        var body = $"Body-{Math.Abs(titleSeed ^ tokenSeed) % 1000}";

        var clock = new MutableTimeProvider(BaselineUtc.AddMinutes(Math.Abs(userSeed) % 60));
        var pendingRepository = new InMemoryPendingNotificationRepository();
        var fcmSender = new FailingFcmPushNotificationSender();
        var notificationService = CreateNotificationService(pendingRepository, fcmSender, clock);
        var retryJob = new RetryFailedNotificationsJob(
            pendingRepository,
            fcmSender,
            clock,
            NullLogger<RetryFailedNotificationsJob>.Instance);

        await notificationService.SendPushAsync(
            userId,
            deviceToken,
            title,
            body,
            NotificationKind.Transactional,
            cancellationToken: CancellationToken.None);

        if (fcmSender.SendCount != 1 || pendingRepository.Items.Count != 1)
        {
            return false;
        }

        var backoffIntervals = new List<TimeSpan>();
        var pending = pendingRepository.Items.Single();
        backoffIntervals.Add(pending.NextRetryAtUtc - pending.LastAttemptAtUtc);

        for (var retry = 0; retry < NotificationRetryPolicy.MaxAttempts - 1; retry++)
        {
            clock.AdvanceTo(pending.NextRetryAtUtc);
            await retryJob.ExecuteAsync(CancellationToken.None);

            if (retry < NotificationRetryPolicy.MaxAttempts - 2)
            {
                if (pendingRepository.Items.Count != 1)
                {
                    return false;
                }

                pending = pendingRepository.Items.Single();
                backoffIntervals.Add(pending.NextRetryAtUtc - pending.LastAttemptAtUtc);
            }
        }

        if (fcmSender.SendCount != NotificationRetryPolicy.MaxAttempts)
        {
            return false;
        }

        if (pendingRepository.Items.Count != 0)
        {
            return false;
        }

        if (backoffIntervals.Count != NotificationRetryPolicy.MaxAttempts - 1)
        {
            return false;
        }

        for (var index = 0; index < backoffIntervals.Count; index++)
        {
            if (backoffIntervals[index] != NotificationRetryPolicy.BackoffIntervals[index])
            {
                return false;
            }
        }

        for (var index = 1; index < backoffIntervals.Count; index++)
        {
            if (backoffIntervals[index] < backoffIntervals[index - 1])
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> RunMarketingOptInScenarioAsync(
        int userSeed,
        bool marketingOptIn,
        bool sendMarketing)
    {
        var userId = BuildUserId(userSeed);
        var user = User.Create(userId, BuildPhoneNumber(userSeed), UserRole.Sender, BaselineUtc);
        user.SetMarketingNotificationsOptIn(marketingOptIn, BaselineUtc);

        var authRepository = new StubUserAuthRepository();
        authRepository.Seed(user);

        var pendingRepository = new InMemoryPendingNotificationRepository();
        var fcmSender = new SuccessfulFcmPushNotificationSender();
        var notificationService = CreateNotificationService(
            pendingRepository,
            fcmSender,
            new MutableTimeProvider(BaselineUtc),
            authRepository);

        var kind = sendMarketing ? NotificationKind.Marketing : NotificationKind.Transactional;

        await notificationService.SendPushAsync(
            userId,
            BuildDeviceToken(userSeed),
            "Promo",
            "Message",
            kind,
            cancellationToken: CancellationToken.None);

        var shouldSend = !sendMarketing || marketingOptIn;
        return fcmSender.SendCount == (shouldSend ? 1 : 0);
    }

    private static NotificationService CreateNotificationService(
        InMemoryPendingNotificationRepository pendingRepository,
        IFcmPushNotificationSender fcmSender,
        MutableTimeProvider clock,
        StubUserAuthRepository? authRepository = null) =>
        new(
            new NoOpSmsNotificationProvider(),
            fcmSender,
            pendingRepository,
            authRepository ?? new StubUserAuthRepository(),
            clock,
            NullLogger<NotificationService>.Instance);

    private static Guid BuildUserId(int seed)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed ^ 0x5A5A5A5A);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed * 31);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
    }

    private static string BuildDeviceToken(int seed) =>
        $"device-token-{Math.Abs(seed):X8}";

    private static string BuildPhoneNumber(int seed) =>
        $"+2782{Math.Abs(seed) % 100000000:D8}";

    private sealed class MutableTimeProvider : TimeProvider
    {
        public MutableTimeProvider(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; private set; }

        public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);

        public void AdvanceTo(DateTime utc) => UtcNow = utc;
    }

    private sealed class InMemoryPendingNotificationRepository : IPendingNotificationRepository
    {
        public List<PendingNotification> Items { get; } = [];

        public Task AddAsync(PendingNotification notification, CancellationToken cancellationToken = default)
        {
            Items.Add(notification);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PendingNotification>> ListDueForRetryAsync(
            DateTime nowUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PendingNotification>>(
                Items
                    .Where(item =>
                        item.NextRetryAtUtc <= nowUtc &&
                        item.AttemptCount < NotificationRetryPolicy.MaxAttempts)
                    .OrderBy(item => item.NextRetryAtUtc)
                    .ToArray());

        public Task RemoveAsync(PendingNotification notification, CancellationToken cancellationToken = default)
        {
            Items.Remove(notification);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FailingFcmPushNotificationSender : IFcmPushNotificationSender
    {
        public int SendCount { get; private set; }

        public Task<FcmSendResult> SendAsync(
            FcmPushMessage message,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(FcmSendResult.Failure("FCM unavailable"));
        }
    }

    private sealed class SuccessfulFcmPushNotificationSender : IFcmPushNotificationSender
    {
        public int SendCount { get; private set; }

        public Task<FcmSendResult> SendAsync(
            FcmPushMessage message,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(FcmSendResult.Success("test-message-id"));
        }
    }

    private sealed class NoOpSmsNotificationProvider : ISmsNotificationProvider
    {
        public Task SendSmsAsync(
            string phoneNumber,
            string message,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubUserAuthRepository : IAuthRepository
    {
        private readonly Dictionary<Guid, User> _users = new();

        public void Seed(User user) => _users[user.Id] = user;

        public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Values.FirstOrDefault(user => user.PhoneNumber == phoneNumber));

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.TryGetValue(userId, out var user) ? user : null);

        public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            _users[user.Id] = user;
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> FindRefreshTokenByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

        public Task<IReadOnlyList<RefreshToken>> ListRefreshTokensForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RefreshToken>>(Array.Empty<RefreshToken>());

        public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddOtpRecordAsync(OtpRecord otpRecord, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
