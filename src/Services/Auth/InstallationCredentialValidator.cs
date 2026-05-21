using System.Security.Cryptography;
using System.Text;
using Core.Enums.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// Hot-path validator for per-installation API credentials, per
/// research.md § R4. Wraps an <see cref="IMemoryCache"/> keyed on the
/// SHA-256 digest of the plaintext (never the plaintext itself).
/// </summary>
/// <remarks>
/// On cache miss the validator iterates over every active credential and
/// PBKDF2-verifies the candidate against each <c>SecretHash</c>. With
/// N active credentials this is O(N · 50 ms) — the cache amortises it.
/// Both hits and misses are cached for a 5-second absolute window so an
/// attacker cannot pin one credential's iteration cost on the request path.
/// <para>
/// A second cache entry — keyed <c>icv-byinst:{installationId}</c> — holds
/// the credential-hash key of each positive resolution so the admin revoke
/// flow can evict by installation id without ever holding the plaintext
/// (FR-014 plaintext-once). Both entries share the same TTL, so natural
/// expiry keeps them in lock-step; this service is registered scoped, so
/// the side-index intentionally lives in the singleton
/// <see cref="IMemoryCache"/> rather than on a per-request field.
/// </para>
/// </remarks>
public class InstallationCredentialValidator : IInstallationCredentialValidator
{
    /// <summary>Absolute TTL for cache entries — the SC-004 ceiling.</summary>
    public static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);

    private const string InstallationKeyPrefix = "icv-byinst:";

    private readonly IMemoryCache _cache;
    private readonly IInstallationApiCredentialRepository _credentials;
    private readonly IPasswordHasher _hasher;

    public InstallationCredentialValidator(
        IMemoryCache cache,
        IInstallationApiCredentialRepository credentials,
        IPasswordHasher hasher)
    {
        _cache = cache;
        _credentials = credentials;
        _hasher = hasher;
    }

    public async Task<int?> ValidateAsync(string plaintext, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return null;
        }

        string key = CacheKey(plaintext);
        if (_cache.TryGetValue(key, out int? cached))
        {
            return cached;
        }

        int? resolved = await ResolveAsync(plaintext, ct).ConfigureAwait(false);

        MemoryCacheEntryOptions entryOptions = new()
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        };
        _cache.Set(key, resolved, entryOptions);

        if (resolved is int installationId)
        {
            // Side-index for Invalidate(int) — same TTL so they age together.
            _cache.Set(InstallationKey(installationId), key,
                new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });
        }

        return resolved;
    }

    public void Invalidate(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return;
        }

        _cache.Remove(CacheKey(plaintext));
    }

    public void Invalidate(int installationId)
    {
        string installationKey = InstallationKey(installationId);
        if (_cache.TryGetValue(installationKey, out string? credentialKey)
            && credentialKey is not null)
        {
            _cache.Remove(credentialKey);
        }
        _cache.Remove(installationKey);
    }

    private async Task<int?> ResolveAsync(string plaintext, CancellationToken ct)
    {
        IReadOnlyList<InstallationApiCredentialEntity> active =
            await _credentials.ListAllActiveAsync(ct).ConfigureAwait(false);

        foreach (InstallationApiCredentialEntity candidate in active)
        {
            if (candidate.Status != InstallationStatus.Active)
            {
                continue;
            }
            if (_hasher.Verify(plaintext, candidate.SecretHash))
            {
                return candidate.InstallationId;
            }
        }
        return null;
    }

    private static string CacheKey(string plaintext)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plaintext));
        return "icv:" + Convert.ToHexString(bytes);
    }

    private static string InstallationKey(int installationId)
        => InstallationKeyPrefix + installationId.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
