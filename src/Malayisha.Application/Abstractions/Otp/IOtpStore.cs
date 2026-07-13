namespace Malayisha.Application.Abstractions.Otp;

public interface IOtpStore
{
    Task StoreHashAsync(
        string phoneNumber,
        string otpHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task<string?> GetHashAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task<long> IncrementAttemptCountAsync(
        string phoneNumber,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    Task<long> GetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task ResetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default);

    Task SetLockoutAsync(
        string phoneNumber,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default);
}
