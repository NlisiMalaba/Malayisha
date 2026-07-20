using FsCheck.Xunit;
using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Application.Features.Auth.DeleteAccount;
using Malayisha.Application.Features.Commission;
using Malayisha.Domain;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Malayisha.Application.Tests;

public sealed class AccountDeletionPropertyTests
{
    private static readonly DateTime BaselineUtc = new(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 100)]
    public bool Property36_AccountDeletionPreservesBookingAndCommissionWithAnonymisedPii(
        int userSeed,
        int bookingSeed,
        int priceSeed)
    {
        return RunAccountDeletionScenarioAsync(userSeed, bookingSeed, priceSeed).GetAwaiter().GetResult();
    }

    private static async Task<bool> RunAccountDeletionScenarioAsync(
        int userSeed,
        int bookingSeed,
        int priceSeed)
    {
        var userId = BuildUserId(userSeed);
        var originalPhone = BuildValidPhoneNumber(userSeed);
        var originalDisplayName = $"Transporter {Math.Abs(userSeed) % 10_000}";
        var originalPhotoUrl = $"https://cdn.example.com/users/{userId:N}/photo.jpg";
        var otherUserId = BuildUserId(userSeed + 1);
        var agreedPrice = (Math.Abs(priceSeed) % 5000) + 100m;

        var user = User.Create(userId, originalPhone, UserRole.Transporter, BaselineUtc);
        user.UpdatePushDeviceToken($"device-{userSeed}", BaselineUtc);

        var profile = TransporterProfile.Create(
            BuildGuid(userSeed ^ 0x1111),
            userId,
            originalDisplayName,
            ["Johannesburg-Cape Town"],
            "Toyota Hilux",
            500m,
            BaselineUtc,
            originalPhotoUrl);

        var booking = Booking.Create(
            BuildGuid(bookingSeed),
            BuildGuid(bookingSeed ^ 0x2222),
            otherUserId,
            userId,
            BaselineUtc,
            $"Booking message {bookingSeed}");

        var commission = CommissionRecord.Create(
            BuildGuid(bookingSeed ^ 0x3333),
            booking.Id,
            userId,
            agreedPrice,
            CommissionConstants.StandardCommissionRate,
            BaselineUtc.AddDays(-1));

        var bookingSnapshot = BookingSnapshot.Capture(booking);
        var commissionSnapshot = CommissionSnapshot.Capture(commission);

        var handler = new DeleteAccountCommandHandler(
            new PropertyAuthRepository(user),
            new PropertyProfileRepository(profile),
            TimeProvider.System,
            NullLogger<DeleteAccountCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteAccountCommand(userId), CancellationToken.None);

        if (!result.IsSuccess)
        {
            return false;
        }

        if (!bookingSnapshot.Matches(booking) || !commissionSnapshot.Matches(commission))
        {
            return false;
        }

        return user.IsDeleted
               && !user.IsActive
               && user.PhoneNumber != originalPhone
               && user.PhoneNumber == AccountAnonymization.CreatePhoneIdentifier(userId)
               && profile.DisplayName != originalDisplayName
               && profile.DisplayName == AccountAnonymization.CreateDisplayNameIdentifier(userId)
               && profile.ProfilePhotoUrl is null
               && user.PushDeviceToken is null;
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
        BitConverter.TryWriteBytes(bytes.AsSpan(4, 4), seed * 7);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 4), seed ^ 0x01020304);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), ~seed);
        return new Guid(bytes);
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

    private readonly record struct BookingSnapshot(
        Guid Id,
        Guid TripListingId,
        Guid? DeliveryRequestId,
        Guid SenderId,
        Guid TransporterId,
        BookingStatus Status,
        decimal? QuotedPriceZar,
        decimal? AgreedPriceZar,
        string? Message,
        DateTime? InTransitAtUtc,
        DateTime? DeliveredAtUtc,
        DateTime? CompletedAtUtc,
        DateTime? CancelledAtUtc,
        Guid? CancelledByUserId,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc)
    {
        public static BookingSnapshot Capture(Booking booking) =>
            new(
                booking.Id,
                booking.TripListingId,
                booking.DeliveryRequestId,
                booking.SenderId,
                booking.TransporterId,
                booking.Status,
                booking.QuotedPriceZar,
                booking.AgreedPriceZar,
                booking.Message,
                booking.InTransitAtUtc,
                booking.DeliveredAtUtc,
                booking.CompletedAtUtc,
                booking.CancelledAtUtc,
                booking.CancelledByUserId,
                booking.CreatedAtUtc,
                booking.UpdatedAtUtc);

        public bool Matches(Booking booking) =>
            Id == booking.Id
            && TripListingId == booking.TripListingId
            && DeliveryRequestId == booking.DeliveryRequestId
            && SenderId == booking.SenderId
            && TransporterId == booking.TransporterId
            && Status == booking.Status
            && QuotedPriceZar == booking.QuotedPriceZar
            && AgreedPriceZar == booking.AgreedPriceZar
            && Message == booking.Message
            && InTransitAtUtc == booking.InTransitAtUtc
            && DeliveredAtUtc == booking.DeliveredAtUtc
            && CompletedAtUtc == booking.CompletedAtUtc
            && CancelledAtUtc == booking.CancelledAtUtc
            && CancelledByUserId == booking.CancelledByUserId
            && CreatedAtUtc == booking.CreatedAtUtc
            && UpdatedAtUtc == booking.UpdatedAtUtc;
    }

    private readonly record struct CommissionSnapshot(
        Guid Id,
        Guid BookingId,
        Guid TransporterUserId,
        decimal AgreedPriceZar,
        decimal CommissionRate,
        decimal CommissionAmountZar,
        CommissionStatus Status,
        Guid? UpdatedByAdminUserId,
        DateTime CompletionDateUtc,
        DateTime? UpdatedAtUtc)
    {
        public static CommissionSnapshot Capture(CommissionRecord record) =>
            new(
                record.Id,
                record.BookingId,
                record.TransporterUserId,
                record.AgreedPriceZar,
                record.CommissionRate,
                record.CommissionAmountZar,
                record.Status,
                record.UpdatedByAdminUserId,
                record.CompletionDateUtc,
                record.UpdatedAtUtc);

        public bool Matches(CommissionRecord record) =>
            Id == record.Id
            && BookingId == record.BookingId
            && TransporterUserId == record.TransporterUserId
            && AgreedPriceZar == record.AgreedPriceZar
            && CommissionRate == record.CommissionRate
            && CommissionAmountZar == record.CommissionAmountZar
            && Status == record.Status
            && UpdatedByAdminUserId == record.UpdatedByAdminUserId
            && CompletionDateUtc == record.CompletionDateUtc
            && UpdatedAtUtc == record.UpdatedAtUtc;
    }

    private sealed class PropertyAuthRepository(User user) : IAuthRepository
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

    private sealed class PropertyProfileRepository(TransporterProfile profile) : ITransporterProfileRepository
    {
        public Task<TransporterProfile?> FindByIdAsync(Guid profileId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransporterProfile?>(profile.Id == profileId ? profile : null);

        public Task<TransporterProfile?> FindByIdForUpdateAsync(
            Guid profileId,
            CancellationToken cancellationToken = default) =>
            FindByIdAsync(profileId, cancellationToken);

        public Task<IReadOnlyDictionary<Guid, TransporterProfile>> FindByIdsAsync(
            IEnumerable<Guid> profileIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, TransporterProfile>>(
                profileIds.Contains(profile.Id)
                    ? new Dictionary<Guid, TransporterProfile> { [profile.Id] = profile }
                    : new Dictionary<Guid, TransporterProfile>());

        public Task<TransporterProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransporterProfile?>(profile.UserId == userId ? profile : null);

        public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.UserId == userId);

        public Task AddAsync(TransporterProfile newProfile, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
