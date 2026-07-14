using FluentValidation;
using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Notifications;
using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.Otp;
using Malayisha.Application.Features.Auth.SendOtp;
using Malayisha.Application.Features.Auth.VerifyOtp;
using Malayisha.Application.Options;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Malayisha.Infrastructure.Otp;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Tests;

public sealed class RegistrationConstraintsPropertyTests
{
    private const string ValidOtpCode = "123456";

    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    private static readonly AuthOtpOptions DefaultOptions = new()
    {
        MaxOtpAttempts = OtpSecurityConstants.DefaultMaxVerifyAttempts,
        LockoutDurationSeconds = OtpSecurityConstants.DefaultLockoutDurationSeconds
    };

    private static readonly SmsTemplateOptions DefaultSmsOptions = new();

    [Property(MaxTest = 100)]
    public bool Property4_SendOtpRegistrationRoleConstraint(int phoneSeed, UserRole role)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var validator = new SendOtpCommandValidator();
        var result = validator.Validate(new SendOtpCommand(phoneNumber, OtpPurpose.Register, role));

        if (AuthValidation.IsAllowedRegistrationRole(role))
        {
            return result.IsValid;
        }

        return !result.IsValid
               && result.Errors.Any(error => error.ErrorCode == AuthErrorCodes.InvalidRole);
    }

    [Property(MaxTest = 100)]
    public bool Property4_VerifyOtpRegistrationRoleConstraint(int phoneSeed, UserRole role)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var validator = new VerifyOtpCommandValidator();
        var result = validator.Validate(new VerifyOtpCommand(
            phoneNumber,
            ValidOtpCode,
            OtpPurpose.Register,
            role));

        if (AuthValidation.IsAllowedRegistrationRole(role))
        {
            return result.IsValid;
        }

        return !result.IsValid
               && result.Errors.Any(error => error.ErrorCode == AuthErrorCodes.InvalidRole);
    }

    [Property(MaxTest = 100)]
    public bool Property5_SendOtpRejectsAlreadyRegisteredPhone(int phoneSeed, UserRole role)
    {
        if (!AuthValidation.IsAllowedRegistrationRole(role))
        {
            return true;
        }

        return RunSendOtpDuplicatePhoneAsync(phoneSeed, role).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property5_VerifyOtpRejectsAlreadyRegisteredPhone(int phoneSeed, UserRole role)
    {
        if (!AuthValidation.IsAllowedRegistrationRole(role))
        {
            return true;
        }

        return RunVerifyOtpDuplicatePhoneAsync(phoneSeed, role).GetAwaiter().GetResult();
    }

    [Property(MaxTest = 100)]
    public bool Property5_NewPhoneRegistrationSendOtpSucceeds(int phoneSeed, UserRole role)
    {
        if (!AuthValidation.IsAllowedRegistrationRole(role))
        {
            return true;
        }

        return RunSendOtpNewPhoneAsync(phoneSeed, role).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunSendOtpDuplicatePhoneAsync(int phoneSeed, UserRole role)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var repository = new RegistrationAuthRepository();
        repository.SeedUser(User.Create(Guid.NewGuid(), phoneNumber, UserRole.Sender, BaselineUtc));

        var handler = CreateSendOtpHandler(repository);
        var result = await handler.Handle(
            new SendOtpCommand(phoneNumber, OtpPurpose.Register, role),
            CancellationToken.None);

        return result.IsError
               && result.ErrorCode == AuthErrorCodes.PhoneAlreadyRegistered;
    }

    private static async Task<bool> RunVerifyOtpDuplicatePhoneAsync(int phoneSeed, UserRole role)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var repository = new RegistrationAuthRepository();
        repository.SeedUser(User.Create(Guid.NewGuid(), phoneNumber, UserRole.Sender, BaselineUtc));

        var otpStore = new InMemoryOtpStore();
        var hasher = new Pbkdf2OtpHasher();
        await otpStore.StoreHashAsync(
            phoneNumber,
            hasher.Hash(phoneNumber, ValidOtpCode),
            TimeSpan.FromMinutes(5));

        var tokenService = new TrackingTokenService();
        var handler = CreateVerifyOtpHandler(otpStore, hasher, repository, tokenService);

        var result = await handler.Handle(
            new VerifyOtpCommand(phoneNumber, ValidOtpCode, OtpPurpose.Register, role),
            CancellationToken.None);

        return result.IsError
               && result.ErrorCode == AuthErrorCodes.PhoneAlreadyRegistered
               && !tokenService.AccessTokenIssued
               && repository.UserAddCount == 0;
    }

    private static async Task<bool> RunSendOtpNewPhoneAsync(int phoneSeed, UserRole role)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var repository = new RegistrationAuthRepository();
        var handler = CreateSendOtpHandler(repository);

        var result = await handler.Handle(
            new SendOtpCommand(phoneNumber, OtpPurpose.Register, role),
            CancellationToken.None);

        return result.IsSuccess;
    }

    private static SendOtpCommandHandler CreateSendOtpHandler(RegistrationAuthRepository repository)
    {
        var otpStore = new InMemoryOtpStore();

        return new SendOtpCommandHandler(
            otpStore,
            new Pbkdf2OtpHasher(),
            new SecureOtpGenerator(),
            new OtpSecurityService(otpStore, Microsoft.Extensions.Options.Options.Create(DefaultOptions)),
            new NoOpNotificationService(),
            repository,
            Microsoft.Extensions.Options.Options.Create(DefaultOptions),
            Microsoft.Extensions.Options.Options.Create(DefaultSmsOptions),
            NullLogger<SendOtpCommandHandler>.Instance);
    }

    private static VerifyOtpCommandHandler CreateVerifyOtpHandler(
        InMemoryOtpStore otpStore,
        IOtpHasher hasher,
        RegistrationAuthRepository repository,
        TrackingTokenService tokenService) =>
        new(
            otpStore,
            hasher,
            new OtpSecurityService(otpStore, Microsoft.Extensions.Options.Options.Create(DefaultOptions)),
            repository,
            tokenService,
            new FixedTimeProvider(BaselineUtc),
            Microsoft.Extensions.Options.Options.Create(DefaultOptions),
            NullLogger<VerifyOtpCommandHandler>.Instance);

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

    private sealed class RegistrationAuthRepository : IAuthRepository
    {
        private readonly Dictionary<string, User> _usersByPhone = new(StringComparer.Ordinal);

        public int UserAddCount { get; private set; }

        public void SeedUser(User user) => _usersByPhone[user.PhoneNumber] = user;

        public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersByPhone.TryGetValue(phoneNumber, out var user) ? user : null);

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersByPhone.Values.FirstOrDefault(user => user.Id == userId));

        public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            UserAddCount++;
            _usersByPhone[user.PhoneNumber] = user;
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> FindRefreshTokenByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(null);

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
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class InMemoryOtpStore : IOtpStore
    {
        private readonly Dictionary<string, string> _hashes = new(StringComparer.Ordinal);

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
}
