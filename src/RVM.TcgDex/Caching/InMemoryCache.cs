using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace RVM.TcgDex.Caching;

/// <summary>
/// Default cache: a dictionary with absolute expiration. Only what the client uses is implemented —
/// absolute expiration, no sliding — which is fine because it is never handed to anyone else.
/// Expired entries are dropped when read and, at most once a minute, swept on write, so a
/// long-running process does not keep every URL it ever asked for.
/// </summary>
internal sealed class InMemoryCache(TimeProvider time) : IDistributedCache
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private long _lastSweepTicks;

    public InMemoryCache() : this(TimeProvider.System)
    {
    }

    internal int Count => _entries.Count;

    public byte[]? Get(string key)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return null;
        if (entry.ExpiresAt > time.GetUtcNow())
            return entry.Value;

        _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
        return null;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var now = time.GetUtcNow();
        var expiresAt = options.AbsoluteExpiration
            ?? (options.AbsoluteExpirationRelativeToNow is { } ttl ? now + ttl : DateTimeOffset.MaxValue);

        _entries[key] = new Entry(value, expiresAt);
        SweepIfDue(now);
    }

    public void Remove(string key) => _entries.TryRemove(key, out _);

    public void Refresh(string key)
    {
        // No sliding expiration: nothing to refresh.
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    private void SweepIfDue(DateTimeOffset now)
    {
        var last = Interlocked.Read(ref _lastSweepTicks);
        if (now.UtcTicks - last < SweepInterval.Ticks || Interlocked.CompareExchange(ref _lastSweepTicks, now.UtcTicks, last) != last)
            return;

        foreach (var pair in _entries)
        {
            if (pair.Value.ExpiresAt <= now)
                _entries.TryRemove(pair);
        }
    }

    private sealed record Entry(byte[] Value, DateTimeOffset ExpiresAt);
}
