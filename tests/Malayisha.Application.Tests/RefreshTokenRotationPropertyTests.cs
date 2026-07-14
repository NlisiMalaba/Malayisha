using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Auth;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.RefreshToken;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class RefreshTokenRotationPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool RefreshRotation_ReturnsNewTokens_AndOriginalCannotBeReused(int phoneSeed, int tokenSeed)
    {
        return RunRotationRoundTripAsync(phoneSeed, tokenSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunRotationRoundTripAsync(int phoneSeed, int tokenSeed)
    {
        var phoneNumber = BuildValidPhoneNumber(phoneSeed);
        var userId = Guid.NewGuid();
        var user = User.Create(userId, phoneNumber, UserRole.Sender, BaselineUtc);

        var tokenService = new RotatingTokenService(tokenSeed);
        var originalRefreshToken = tokenService.GenerateRefreshToken();
        var originalHash = tokenService.HashRefreshToken(originalRefreshToken);

        var repository = new InMemoryAuthRepository();
        repository.SeedUser(user);
        repository.SeedRefreshToken(RefreshToken.Create(
            Guid.NewGuid(),
            userId,
            originalHash,
            BaselineUtc,
            BaselineUtc.AddDays(30)));

        var handler = new RefreshTokenCommandHandler(
            repository,
            tokenService,
            new FixedTimeProvider(BaselineUtc),
            NullLogger<RefreshTokenCommandHandler>.Instance);

        var firstResult = await handler.Handle(
            new RefreshTokenCommand(originalRefreshToken),
            CancellationToken.None);

        if (firstResult.IsError || firstResult.Value is null)
        {
            return false;
        }

        var session = firstResult.Value;

        if (string.IsNullOrWhiteSpace(session.AccessToken)
            || string.IsNullOrWhiteSpace(session.RefreshToken)
            || session.RefreshToken == originalRefreshToken
            || session.UserId != userId
            || session.PhoneNumber != phoneNumber)
        {
            return false;
        }

        var reuseResult = await handler.Handle(
            new RefreshTokenCommand(originalRefreshToken),
            CancellationToken.None);

        return reuseResult.IsError
               && reuseResult.ErrorCode == AuthErrorCodes.RefreshTokenAlreadyUsed
               && reuseResult.Value is null;
    }

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

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class RotatingTokenService(int seed) : ITokenService
    {
        private int _sequence;

        public int RefreshTokenLifetimeDays => 30;

        public AccessTokenResult CreateAccessToken(Guid userId, string phoneNumber, UserRole role)
        {
            var token = $"access-{userId:N}-{Interlocked.Increment(ref _sequence)}-{Math.Abs(seed)}";
            return new AccessTokenResult(token, 900);
        }

        public string GenerateRefreshToken()
        {
            var index = Interlocked.Increment(ref _sequence);
            return $"refresh-{Math.Abs(seed):x8}-{index:D4}";
        }

        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
    }

    private sealed class InMemoryAuthRepository : IAuthRepository
    {
        private readonly Dictionary<Guid, User> _usersById = new();
        private readonly Dictionary<string, User> _usersByPhone = new(StringComparer.Ordinal);
        private readonly Dictionary<string, RefreshToken> _refreshTokensByHash = new(StringComparer.Ordinal);

        public void SeedUser(User user)
        {
            _usersById[user.Id] = user;
            _usersByPhone[user.PhoneNumber] = user;
        }

        public void SeedRefreshToken(RefreshToken refreshToken) =>
            _refreshTokensByHash[refreshToken.TokenHash] = refreshToken;

        public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersByPhone.TryGetValue(phoneNumber, out var user) ? user : null);

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usersById.TryGetValue(userId, out var user) ? user : null);

        public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            SeedUser(user);
            return Task.CompletedTask;
        }

        public Task<RefreshToken?> FindRefreshTokenByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_refreshTokensByHash.TryGetValue(tokenHash, out var token) ? token : null);

        public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            _refreshTokensByHash[refreshToken.TokenHash] = refreshToken;
            return Task.CompletedTask;
        }

        public Task AddOtpRecordAsync(OtpRecord otpRecord, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
