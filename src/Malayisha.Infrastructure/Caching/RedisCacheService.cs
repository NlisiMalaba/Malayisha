using System.Text.Json;
using Malayisha.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace Malayisha.Infrastructure.Caching;

internal sealed class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDatabase _database = connectionMultiplexer.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        var value = await _database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(value.ToString()!, SerializerOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        await _database.StringSetAsync(key, payload, ttl).WaitAsync(cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        await _database.KeyDeleteAsync(key).WaitAsync(cancellationToken);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        await _database.KeyExistsAsync(key).WaitAsync(cancellationToken);
}
