using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.Otp;
using Malayisha.Application.Features.Auth.SendOtp;
using Malayisha.Application.Options;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Otp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Tests;

public sealed class OtpPlaintextStoragePropertyTests
{
    private static readonly AuthOtpOptions DefaultOptions = new()
    {
        MaxOtpAttempts = OtpSecurityConstants.DefaultMaxVerifyAttempts,
        LockoutDurationSeconds = OtpSecurityConstants.DefaultLockoutDurationSeconds
    };

    private static readonly SmsTemplateOptions DefaultSmsOptions = new();

    [Property(MaxTest = 100)]
    public bool Property35_OtpIssuance_StoresHashDistinctFromPlaintext(int phoneSeed, int otpSeed)
    {
        return RunOtpStorageScenarioAsync(phoneSeed, otpSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunOtpStorageScenarioAsync(int phoneSeed, int otpSeed)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var otpCode = BuildOtpCode(otpSeed);
        var otpStore = new InspectingOtpStore();
        var hasher = new Pbkdf2OtpHasher();
        var repository = new SendOtpAuthRepository();

        repository.SeedUser(User.Create(
            Guid.NewGuid(),
            phoneNumber,
            UserRole.Sender,
            DateTime.UtcNow));

        var handler = new SendOtpCommandHandler(
            otpStore,
            hasher,
            new FixedOtpGenerator(otpCode),
            new OtpSecurityService(otpStore, Microsoft.Extensions.Options.Options.Create(DefaultOptions)),
            new NoOpNotificationService(),
            repository,
            Microsoft.Extensions.Options.Options.Create(DefaultOptions),
            Microsoft.Extensions.Options.Options.Create(DefaultSmsOptions),
            NullLogger<SendOtpCommandHandler>.Instance);

        var result = await handler.Handle(
            new SendOtpCommand(phoneNumber, OtpPurpose.Login),
            CancellationToken.None);

        if (!result.IsSuccess)
        {
            return false;
        }

        var storedValue = otpStore.GetStoredHash(phoneNumber);
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return false;
        }

        return storedValue != otpCode
               && !storedValue.Contains(otpCode, StringComparison.Ordinal)
               && IsLikelyHash(storedValue)
               && hasher.Verify(phoneNumber, otpCode, storedValue);
    }

    private static bool IsLikelyHash(string value)
    {
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string BuildValidPhoneNumber(int seed)
    {
        var value = Math.Abs(seed);
        var firstDigit = (value % 9) + 1;
        var length = (value % 14) + 1;
        Span<char> digits = stackalloc char[length];

        for (var index = 0; index < length; index++)
        {
            digits[index] = (char)('0' + ((value / (index + 1)) % 10));
        }

        return $"+{firstDigit}{new string(digits)}";
    }

    private static string BuildOtpCode(int seed) =>
        ((Math.Abs(seed) % 999_999) + 1).ToString("D6");

    private sealed class FixedOtpGenerator(string otpCode) : IOtpGenerator
    {
        public string Generate() => otpCode;
    }

    private sealed class InspectingOtpStore : IOtpStore
    {
        private readonly Dictionary<string, string> _hashes = new(StringComparer.Ordinal);

        public string? GetStoredHash(string phoneNumber) =>
            _hashes.TryGetValue(phoneNumber, out var hash) ? hash : null;

        public Task StoreHashAsync(
            string phoneNumber,
            string otpHash,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            _hashes[phoneNumber] = otpHash;
            return Task.CompletedTask;
        }

        public Task<string?> GetHashAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(GetStoredHash(phoneNumber));

        public Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            _hashes.Remove(phoneNumber);
            return Task.CompletedTask;
        }

        public Task<long> IncrementAttemptCountAsync(
            string phoneNumber,
            TimeSpan ttl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(1L);

        public Task<long> GetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task ResetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SetLockoutAsync(
            string phoneNumber,
            TimeSpan duration,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> TryRecordSendAsync(
            string phoneNumber,
            int maxSends,
            TimeSpan window,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class SendOtpAuthRepository : IAuthRepository
    {
        private readonly Dictionary<string, User> _usersByPhone = new(StringComparer.Ordinal);

        public void SeedUser(User user) => _usersByPhone[user.PhoneNumber] = user;

        public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersByPhone.TryGetValue(phoneNumber, out var user) ? user : null);

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersByPhone.Values.FirstOrDefault(user => user.Id == userId));

        public Task AddUserAsync(User user, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

    private sealed class NoOpNotificationService : INotificationService
    {
        public Task SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SendPushAsync(
            Guid userId,
            string deviceToken,
            string title,
            string body,
            NotificationKind kind = NotificationKind.Transactional,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
