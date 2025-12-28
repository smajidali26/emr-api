using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace EMR.Infrastructure.Services;

/// <summary>
/// Redis cache service for distributed caching
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(cached))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(cached);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions();

        if (absoluteExpiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }

        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        return !string.IsNullOrEmpty(cached);
    }
}

/// <summary>
/// Cache service interface
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
