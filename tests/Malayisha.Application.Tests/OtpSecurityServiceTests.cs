using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Features.Auth.Otp;
using Malayisha.Application.Options;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Tests;

public sealed class OtpSecurityServiceTests
{
    private static readonly AuthOtpOptions DefaultOptions = new()
    {
        MaxOtpAttempts = 5,
        LockoutDurationSeconds = 900,
        MaxOtpSendRequests = 3,
        OtpSendRateLimitWindowSeconds = 900
    };

    [Fact]
    public async Task RecordFailedVerification_OnFifthAttempt_ReturnsPhoneLockedOut()
    {
        var store = new FakeOtpStore();
        var service = CreateService(store);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var error = await service.RecordFailedVerificationAsync("+27123456789");
            Assert.Equal(AuthErrorCodes.InvalidOtp, error);
        }

        var lockoutError = await service.RecordFailedVerificationAsync("+27123456789");

        Assert.Equal(AuthErrorCodes.PhoneLockedOut, lockoutError);
        Assert.True(store.LockoutSet);
    }

    [Fact]
    public async Task TryRecordSend_WhenLimitExceeded_ReturnsRateLimited()
    {
        var store = new FakeOtpStore { SendCount = 3 };
        var service = CreateService(store);

        var error = await service.TryRecordSendAsync("+27123456789");

        Assert.Equal(AuthErrorCodes.OtpSendRateLimited, error);
    }

    [Fact]
    public async Task TryRecordSend_WhenLockedOut_ReturnsPhoneLockedOut()
    {
        var store = new FakeOtpStore { IsLockedOut = true };
        var service = CreateService(store);

        var error = await service.TryRecordSendAsync("+27123456789");

        Assert.Equal(AuthErrorCodes.PhoneLockedOut, error);
    }

    [Fact]
    public async Task GetLockoutError_WhenLockedOut_ReturnsPhoneLockedOut()
    {
        var store = new FakeOtpStore { IsLockedOut = true };
        var service = CreateService(store);

        var error = await service.GetLockoutErrorAsync("+27123456789");

        Assert.Equal(AuthErrorCodes.PhoneLockedOut, error);
    }

    private static OtpSecurityService CreateService(FakeOtpStore store) =>
        new(store, Microsoft.Extensions.Options.Options.Create(DefaultOptions));

    private sealed class FakeOtpStore : IOtpStore
    {
        public bool IsLockedOut { get; set; }

        public long SendCount { get; set; }

        public bool LockoutSet { get; private set; }

        public Task StoreHashAsync(string phoneNumber, string otpHash, TimeSpan ttl, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string?> GetHashAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<long> IncrementAttemptCountAsync(string phoneNumber, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(SendCount);
        }

        public Task<long> GetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(SendCount);

        public Task ResetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            SendCount = 0;
            return Task.CompletedTask;
        }

        public Task SetLockoutAsync(string phoneNumber, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            LockoutSet = true;
            IsLockedOut = true;
            return Task.CompletedTask;
        }

        public Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
            Task.FromResult(IsLockedOut);

        public Task<bool> TryRecordSendAsync(
            string phoneNumber,
            int maxSends,
            TimeSpan window,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.FromResult(SendCount <= maxSends);
        }
    }
}
