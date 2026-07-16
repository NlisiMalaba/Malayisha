using Malayisha.Application.Abstractions.Chat;
using StackExchange.Redis;

namespace Malayisha.Infrastructure.Chat;

internal sealed class RedisChatPresenceTracker(IConnectionMultiplexer redis) : IChatPresenceTracker
{
    private static readonly TimeSpan ConnectionExpiry = TimeSpan.FromHours(24);

    public async Task ConnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var key = GetPresenceKey(userId);
        await database.SetAddAsync(key, connectionId);
        await database.KeyExpireAsync(key, ConnectionExpiry);
    }

    public async Task DisconnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        var key = GetPresenceKey(userId);
        await database.SetRemoveAsync(key, connectionId);

        if (await database.SetLengthAsync(key) == 0)
        {
            await database.KeyDeleteAsync(key);
        }
    }

    public async Task<bool> IsUserConnectedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();
        return await database.SetLengthAsync(GetPresenceKey(userId)) > 0;
    }

    private static RedisKey GetPresenceKey(Guid userId) => $"chat:presence:{userId}";
}
