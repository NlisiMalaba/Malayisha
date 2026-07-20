using FsCheck;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.Otp;
using Malayisha.Application.Features.Auth.VerifyOtp;
using Malayisha.Application.Options;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Otp;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class OtpLockoutPropertyTests
{
    private const string ValidOtpCode = "123456";

    private static readonly AuthOtpOptions DefaultOptions = new()
    {
        MaxOtpAttempts = OtpSecurityConstants.DefaultMaxVerifyAttempts,
        LockoutDurationSeconds = OtpSecurityConstants.DefaultLockoutDurationSeconds
    };

    [Property(MaxTest = 100)]
    public bool RepeatedInvalidOtps_EventuallyLockOutAndNeverIssueJwt(int phoneSeed, int extraAttempts)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var attemptCount = OtpSecurityConstants.DefaultMaxVerifyAttempts + Math.Abs(extraAttempts % 10);
        return RunLockoutScenarioAsync(phoneNumber, attemptCount).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool AfterLockout_CorrectOtpStillRejected(int phoneSeed)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        return RunPostLockoutCorrectOtpRejectedAsync(phoneNumber).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunLockoutScenarioAsync(string phoneNumber, int attemptCount)
    {
        var otpStore = new InMemoryOtpStore();
        var hasher = new Pbkdf2OtpHasher();
        var tokenService = new TrackingTokenService();
        var handler = CreateHandler(otpStore, hasher, tokenService);

        await otpStore.StoreHashAsync(
            phoneNumber,
            hasher.Hash(phoneNumber, ValidOtpCode),
            TimeSpan.FromMinutes(5));

        for (var attempt = 0; attempt < attemptCount; attempt++)
        {
            var wrongOtp = WrongOtpCode(attempt);
            var result = await handler.Handle(
                new VerifyOtpCommand(phoneNumber, wrongOtp, OtpPurpose.Login),
                CancellationToken.None);

            if (result.IsSuccess)
            {
                return false;
            }

            var expectedError = attempt < OtpSecurityConstants.DefaultMaxVerifyAttempts - 1
                ? AuthErrorCodes.InvalidOtp
                : AuthErrorCodes.PhoneLockedOut;

            if (result.ErrorCode != expectedError)
            {
                return false;
            }
        }

        return !tokenService.AccessTokenIssued && otpStore.IsLockedOut(phoneNumber);
    }

    private static async Task<bool> RunPostLockoutCorrectOtpRejectedAsync(string phoneNumber)
    {
        var otpStore = new InMemoryOtpStore();
        var hasher = new Pbkdf2OtpHasher();
        var tokenService = new TrackingTokenService();
        var handler = CreateHandler(otpStore, hasher, tokenService);

        await otpStore.StoreHashAsync(
            phoneNumber,
            hasher.Hash(phoneNumber, ValidOtpCode),
            TimeSpan.FromMinutes(5));

        for (var attempt = 0; attempt < OtpSecurityConstants.DefaultMaxVerifyAttempts; attempt++)
        {
            _ = await handler.Handle(
                new VerifyOtpCommand(phoneNumber, WrongOtpCode(attempt), OtpPurpose.Login),
                CancellationToken.None);
        }

        var result = await handler.Handle(
            new VerifyOtpCommand(phoneNumber, ValidOtpCode, OtpPurpose.Login),
            CancellationToken.None);

        return result.IsError
               && result.ErrorCode == AuthErrorCodes.PhoneLockedOut
               && !tokenService.AccessTokenIssued;
    }

    private static VerifyOtpCommandHandler CreateHandler(
        InMemoryOtpStore otpStore,
        IOtpHasher hasher,
        TrackingTokenService tokenService) =>
        new(
            otpStore,
            hasher,
            new OtpSecurityService(otpStore, Microsoft.Extensions.Options.Options.Create(DefaultOptions)),
            new StubAuthRepository(phoneNumber => User.Create(
                Guid.NewGuid(),
                phoneNumber,
                UserRole.Sender,
                DateTime.UtcNow)),
            tokenService,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(DefaultOptions),
            NullLogger<VerifyOtpCommandHandler>.Instance);

    private static string BuildValidPhoneNumber(int seed)
    {
        var value = Math.Abs(seed);
        var firstDigit = (value % 9) + 1;
        var length = (value % 13) + 1;
        Span<char> digits = stackalloc char[length];

        for (var index = 0; index < length; index++)
        {
            digits[index] = (char)('0' + ((value / (index + 1)) % 10));
        }

        return $"+{firstDigit}{new string(digits)}";
    }

    private static string WrongOtpCode(int attempt)
    {
        var candidate = ((attempt + 1) * 111_111 % 1_000_000).ToString("D6");
        return candidate == ValidOtpCode ? "654321" : candidate;
    }

    private sealed class InMemoryOtpStore : IOtpStore
    {
        private readonly Dictionary<string, string> _hashes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _attempts = new(StringComparer.Ordinal);
        private readonly HashSet<string> _lockouts = new(StringComparer.Ordinal);

        public bool IsLockedOut(string phoneNumber) => _lockouts.Contains(phoneNumber);

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
            Task.FromResult(_hashes.TryGetValue(phoneNumber, out var hash) ? hash : null);

        public Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            _hashes.Remove(phoneNumber);
            _attempts.Remove(phoneNumber);
            return Task.CompletedTask;
        }

        public Task<long> IncrementAttemptCountAsync(
            string phoneNumber,
            TimeSpan ttl,
            CancellationToken cancellationToken = default)
        {
            _attempts.TryGetValue(phoneNumber, out var count);
            count++;
            _attempts[phoneNumber] = count;
            return Task.FromResult(count);
        }

        public Task<long> GetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_attempts.TryGetValue(phoneNumber, out var count) ? count : 0);

        public Task ResetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            _attempts.Remove(phoneNumber);
            return Task.CompletedTask;
        }

        public Task SetLockoutAsync(
            string phoneNumber,
            TimeSpan duration,
            CancellationToken cancellationToken = default)
        {
            _ = duration;
            _lockouts.Add(phoneNumber);
            return Task.CompletedTask;
        }

        public Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_lockouts.Contains(phoneNumber));

        public Task<bool> TryRecordSendAsync(
            string phoneNumber,
            int maxSends,
            TimeSpan window,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class TrackingTokenService : ITokenService
    {
        public bool AccessTokenIssued { get; private set; }

        public int RefreshTokenLifetimeDays => 30;

        public AccessTokenResult CreateAccessToken(Guid userId, string phoneNumber, UserRole role)
        {
            AccessTokenIssued = true;
            return new AccessTokenResult("issued-access-token", 900);
        }

        public string GenerateRefreshToken() => "issued-refresh-token";

        public string HashRefreshToken(string refreshToken) => refreshToken;
    }

    private sealed class StubAuthRepository(Func<string, User> userFactory) : IAuthRepository
    {
        public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(userFactory(phoneNumber));

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

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
}
