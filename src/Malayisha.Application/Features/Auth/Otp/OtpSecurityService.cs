using Malayisha.Application.Abstractions.Otp;
using Malayisha.Application.Features.Auth;
using Malayisha.Application.Options;
using Microsoft.Extensions.Options;

namespace Malayisha.Application.Features.Auth.Otp;

public interface IOtpSecurityService
{
    Task<string?> GetLockoutErrorAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<string?> TryRecordSendAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<string?> RecordFailedVerificationAsync(string phoneNumber, CancellationToken cancellationToken = default);
}

internal sealed class OtpSecurityService(
    IOtpStore otpStore,
    IOptions<AuthOtpOptions> otpOptions) : IOtpSecurityService
{
    public Task<string?> GetLockoutErrorAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        IsLockedOutAsync(phoneNumber, cancellationToken);

    public async Task<string?> TryRecordSendAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var lockoutError = await IsLockedOutAsync(phoneNumber, cancellationToken);
        if (lockoutError is not null)
        {
            return lockoutError;
        }

        var options = otpOptions.Value;
        var sendAllowed = await otpStore.TryRecordSendAsync(
            phoneNumber,
            options.MaxOtpSendRequests,
            TimeSpan.FromSeconds(options.OtpSendRateLimitWindowSeconds),
            cancellationToken);

        return sendAllowed ? null : AuthErrorCodes.OtpSendRateLimited;
    }

    public async Task<string?> RecordFailedVerificationAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var options = otpOptions.Value;
        var lockoutWindow = TimeSpan.FromSeconds(options.LockoutDurationSeconds);
        var attempts = await otpStore.IncrementAttemptCountAsync(
            phoneNumber,
            lockoutWindow,
            cancellationToken);

        if (attempts >= options.MaxOtpAttempts)
        {
            await otpStore.SetLockoutAsync(phoneNumber, lockoutWindow, cancellationToken);
            return AuthErrorCodes.PhoneLockedOut;
        }

        return AuthErrorCodes.InvalidOtp;
    }

    private async Task<string?> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        if (await otpStore.IsLockedOutAsync(phoneNumber, cancellationToken))
        {
            return AuthErrorCodes.PhoneLockedOut;
        }

        return null;
    }
}
