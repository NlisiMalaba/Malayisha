using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.DeleteAccount;
using Malayisha.Domain;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class DeleteAccountCommandHandlerTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_AnonymisesUserAndProfile_RevokesRefreshTokens_AndRetainsUserId()
    {
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "+27821234567", UserRole.Transporter, NowUtc);
        user.UpdatePushDeviceToken("device-token", NowUtc);

        var profile = TransporterProfile.Create(
            Guid.NewGuid(),
            userId,
            "Alice Transporter",
            ["Johannesburg-Cape Town"],
            "Toyota Hilux",
            500m,
            NowUtc,
            "https://cdn.example.com/photo.jpg");

        var refreshToken = RefreshToken.Create(
            Guid.NewGuid(),
            userId,
            "refresh-hash",
            NowUtc,
            NowUtc.AddDays(30));

        var authRepository = new InMemoryAuthRepository(user, [refreshToken]);
        var profileRepository = new InMemoryDeleteAccountProfileRepository(profile);
        var handler = new DeleteAccountCommandHandler(
            authRepository,
            profileRepository,
            TimeProvider.System,
            NullLogger<DeleteAccountCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteAccountCommand(userId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsDeleted);
        Assert.False(user.IsActive);
        Assert.Equal(AccountAnonymization.CreatePhoneIdentifier(userId), user.PhoneNumber);
        Assert.Null(user.PushDeviceToken);
        Assert.Equal(AccountAnonymization.CreateDisplayNameIdentifier(userId), profile.DisplayName);
        Assert.Null(profile.ProfilePhotoUrl);
        Assert.True(refreshToken.IsRevoked);
        Assert.Equal(userId, profile.UserId);
    }

    [Fact]
    public async Task Handle_WhenAlreadyDeleted_ReturnsAccountAlreadyDeleted()
    {
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "+27821234567", UserRole.Sender, NowUtc);
        user.AnonymizeAndDelete(AccountAnonymization.CreatePhoneIdentifier(userId), NowUtc);

        var handler = new DeleteAccountCommandHandler(
            new InMemoryAuthRepository(user, []),
            new InMemoryDeleteAccountProfileRepository(),
            TimeProvider.System,
            NullLogger<DeleteAccountCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteAccountCommand(userId), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Equal(AccountErrorCodes.AccountAlreadyDeleted, result.ErrorCode);
    }

    private sealed class InMemoryAuthRepository(User user, IReadOnlyList<RefreshToken> refreshTokens) : IAuthRepository
    {
        public Task<User?> FindUserByPhoneAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.PhoneNumber == phoneNumber ? user : null);

        public Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.Id == userId ? user : null);

        public Task AddUserAsync(User newUser, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<RefreshToken?> FindRefreshTokenByHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<RefreshToken?>(refreshTokens.FirstOrDefault(token => token.TokenHash == tokenHash));

        public Task<IReadOnlyList<RefreshToken>> ListRefreshTokensForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RefreshToken>>(
                refreshTokens.Where(token => token.UserId == userId).ToArray());

        public Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddOtpRecordAsync(OtpRecord otpRecord, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryDeleteAccountProfileRepository(TransporterProfile? profile = null)
        : ITransporterProfileRepository
    {
        public Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile?.Id == profileId ? profile : null);

        public Task<TransporterProfile?> FindByIdForUpdateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(profileId, cancellationToken);

        public Task<IReadOnlyDictionary<Guid, TransporterProfile>> FindByIdsAsync(
            IEnumerable<Guid> profileIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, TransporterProfile>>(
                profile is null
                    ? new Dictionary<Guid, TransporterProfile>()
                    : profileIds.Contains(profile.Id)
                        ? new Dictionary<Guid, TransporterProfile> { [profile.Id] = profile }
                        : new Dictionary<Guid, TransporterProfile>());

        public Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile?.UserId == userId ? profile : null);

        public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile?.UserId == userId);

        public Task AddAsync(TransporterProfile newProfile, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
