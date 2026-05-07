using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Tests.Unit.Services.Auth.Fakes;

/// <summary>
/// Minimal manual fake for <see cref="IMemoryCache"/> with a controllable
/// clock — supports the TTL semantics the validator relies on, plus a
/// public <see cref="Keys"/> projection so tests can assert that the
/// plaintext does not appear as a raw key (R4).
/// </summary>
internal sealed class FakeMemoryCache : IMemoryCache
{
    private readonly Dictionary<object, Entry> _entries = [];

    public DateTimeOffset Now { get; set; } = DateTimeOffset.UnixEpoch;

    public IReadOnlyCollection<object> Keys => _entries.Keys;

    public void Advance(TimeSpan delta) => Now += delta;

    public ICacheEntry CreateEntry(object key) => new FakeCacheEntry(this, key);

    public bool TryGetValue(object key, out object? value)
    {
        if (_entries.TryGetValue(key, out Entry entry) && entry.ExpiresAt > Now)
        {
            value = entry.Value;
            return true;
        }
        _entries.Remove(key);
        value = null;
        return false;
    }

    public void Remove(object key) => _entries.Remove(key);

    public void Dispose() => _entries.Clear();

    internal void Commit(object key, object? value, DateTimeOffset expiresAt)
        => _entries[key] = new Entry(value, expiresAt);

    private readonly record struct Entry(object? Value, DateTimeOffset ExpiresAt);

    private sealed class FakeCacheEntry : ICacheEntry
    {
        private readonly FakeMemoryCache _parent;
        private bool _disposed;

        public FakeCacheEntry(FakeMemoryCache parent, object key)
        {
            _parent = parent;
            Key = key;
            ExpirationTokens = [];
            PostEvictionCallbacks = [];
        }

        public object Key { get; }
        public object? Value { get; set; }
        public DateTimeOffset? AbsoluteExpiration { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public IList<IChangeToken> ExpirationTokens { get; }
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks { get; }
        public CacheItemPriority Priority { get; set; }
        public long? Size { get; set; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            DateTimeOffset expiresAt = AbsoluteExpirationRelativeToNow.HasValue
                ? _parent.Now + AbsoluteExpirationRelativeToNow.Value
                : AbsoluteExpiration ?? DateTimeOffset.MaxValue;
            _parent.Commit(Key, Value, expiresAt);
        }
    }
}
