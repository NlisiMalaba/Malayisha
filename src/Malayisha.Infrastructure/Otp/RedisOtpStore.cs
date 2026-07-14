using Malayisha.Application.Abstractions.Caching;
using Malayisha.Application.Abstractions.Otp;
using StackExchange.Redis;

namespace Malayisha.Infrastructure.Otp;

internal sealed class RedisOtpStore(IConnectionMultiplexer connectionMultiplexer) : IOtpStore
{
    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task StoreHashAsync(
        string phoneNumber,
        string otpHash,
        TimeSpan ttl,
        CancellationToken cancellationToken = default) =>
        await _database.StringSetAsync(RedisKeys.Otp(phoneNumber), otpHash, ttl)
            .WaitAsync(cancellationToken);

    public async Task<string?> GetHashAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(RedisKeys.Otp(phoneNumber)).WaitAsync(cancellationToken);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task RemoveAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(
            [
                (RedisKey)RedisKeys.Otp(phoneNumber),
                (RedisKey)RedisKeys.OtpAttempts(phoneNumber)
            ]).WaitAsync(cancellationToken);
    }

    public async Task<long> IncrementAttemptCountAsync(
        string phoneNumber,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var key = (RedisKey)RedisKeys.OtpAttempts(phoneNumber);
        var count = await _database.StringIncrementAsync(key).WaitAsync(cancellationToken);

        if (count == 1)
        {
            await _database.KeyExpireAsync(key, ttl).WaitAsync(cancellationToken);
        }

        return count;
    }

    public async Task<long> GetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(RedisKeys.OtpAttempts(phoneNumber))
            .WaitAsync(cancellationToken);

        return value.IsNullOrEmpty ? 0 : (long)value;
    }

    public async Task ResetAttemptCountAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        await _database.KeyDeleteAsync(RedisKeys.OtpAttempts(phoneNumber)).WaitAsync(cancellationToken);

    public async Task SetLockoutAsync(
        string phoneNumber,
        TimeSpan duration,
        CancellationToken cancellationToken = default) =>
        await _database.StringSetAsync(RedisKeys.OtpLockout(phoneNumber), "1", duration)
            .WaitAsync(cancellationToken);

    public async Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default) =>
        await _database.KeyExistsAsync(RedisKeys.OtpLockout(phoneNumber)).WaitAsync(cancellationToken);

    public async Task<bool> TryRecordSendAsync(
        string phoneNumber,
        int maxSends,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var key = (RedisKey)RedisKeys.OtpSendRate(phoneNumber);
        var count = await _database.StringIncrementAsync(key).WaitAsync(cancellationToken);

        if (count == 1)
        {
            await _database.KeyExpireAsync(key, window).WaitAsync(cancellationToken);
        }

        return count <= maxSends;
    }
}
