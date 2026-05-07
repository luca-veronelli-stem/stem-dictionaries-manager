using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// Bootstrap-token mint, lookup, and state-transition service. Wraps
/// <see cref="IBootstrapTokenRepository"/>, <see cref="IPasswordHasher"/>,
/// and <see cref="ITokenGenerator"/> behind <see cref="IBootstrapTokenService"/>.
/// The plaintext is returned only by <see cref="MintAsync"/> and is never
/// persisted — only its PBKDF2 hash is (data-model invariant 4).
/// </summary>
public class BootstrapTokenService : IBootstrapTokenService
{
    /// <summary>Default mint TTL when the request omits <c>ttlHours</c> — 30 days, per FR-007 / contract.</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(30);

    private readonly IBootstrapTokenRepository _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator? _generator;
    private readonly AppDbContext? _db;
    private readonly TimeProvider _time;

    /// <summary>
    /// Constructor used for the mint path (US2). <see cref="MintAsync"/>
    /// must <c>SaveChangesAsync</c> on the shared <see cref="AppDbContext"/>
    /// so the new entity's identity column is populated before the endpoint
    /// can audit it; within an open transaction this stages the row but
    /// does not commit. The lookup/mark-used constructor below is preserved
    /// for unit tests that exercise the US1 surface without paying for a
    /// token generator or a real DbContext.
    /// </summary>
    public BootstrapTokenService(IBootstrapTokenRepository tokens,
        IPasswordHasher hasher, ITokenGenerator generator, AppDbContext db,
        TimeProvider? time = null)
    {
        _tokens = tokens;
        _hasher = hasher;
        _generator = generator;
        _db = db;
        _time = time ?? TimeProvider.System;
    }

    public BootstrapTokenService(IBootstrapTokenRepository tokens, IPasswordHasher hasher)
    {
        _tokens = tokens;
        _hasher = hasher;
        _generator = null;
        _db = null;
        _time = TimeProvider.System;
    }

    public async Task<(BootstrapToken Record, string Plaintext)> MintAsync(string clientApp,
        TimeSpan? ttl, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientApp);
        if (_generator is null || _db is null)
        {
            throw new InvalidOperationException(
                "BootstrapTokenService was constructed without an ITokenGenerator or AppDbContext; " +
                "MintAsync requires the five-arg ctor.");
        }

        TimeSpan effective = ttl ?? DefaultTtl;
        if (effective < BootstrapToken.MinTtl || effective > BootstrapToken.MaxTtl)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl),
                $"TTL must be within [{BootstrapToken.MinTtl}, {BootstrapToken.MaxTtl}] (FR-007); was {effective}.");
        }

        DateTime mintedAt = _time.GetUtcNow().UtcDateTime;
        DateTime expiresAt = mintedAt + effective;
        string plaintext = _generator.GenerateBootstrapToken();
        string hash = _hasher.Hash(plaintext);

        BootstrapTokenEntity entity = new()
        {
            ClientApp = clientApp,
            SecretHash = hash,
            MintedAt = mintedAt,
            ExpiresAt = expiresAt,
            Status = BootstrapTokenStatus.Issued
        };

        await _tokens.AddAsync(entity, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return (ToDomain(entity), plaintext);
    }

    public async Task<BootstrapToken?> LookupAsync(string plaintext, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return null;
        }

        IReadOnlyList<BootstrapTokenEntity> active = await _tokens
            .ListByStatusAsync(BootstrapTokenStatus.Issued, ct)
            .ConfigureAwait(false);

        foreach (BootstrapTokenEntity candidate in active)
        {
            if (_hasher.Verify(plaintext, candidate.SecretHash))
            {
                return ToDomain(candidate);
            }
        }
        return null;
    }

    public async Task MarkUsedAsync(int tokenId, int installationId, DateTime usedAt,
        CancellationToken ct = default)
    {
        BootstrapTokenEntity? entity = await _tokens.GetByIdAsync(tokenId, ct).ConfigureAwait(false);
        if (entity is null)
        {
            throw new InvalidOperationException(
                $"BootstrapToken {tokenId} not found; cannot mark as Used.");
        }

        BootstrapToken domain = ToDomain(entity);
        domain.MarkUsed(usedAt, installationId);

        entity.Status = domain.Status;
        entity.UsedAt = domain.UsedAt;
        entity.ConsumedByInstallationId = domain.ConsumedByInstallationId;

        await _tokens.UpdateAsync(entity, ct).ConfigureAwait(false);
    }

    private static BootstrapToken ToDomain(BootstrapTokenEntity entity)
        => BootstrapToken.Restore(
            id: entity.Id,
            clientApp: entity.ClientApp,
            secretHash: entity.SecretHash,
            mintedAt: entity.MintedAt,
            expiresAt: entity.ExpiresAt,
            status: entity.Status,
            usedAt: entity.UsedAt,
            consumedByInstallationId: entity.ConsumedByInstallationId,
            revokedAt: entity.RevokedAt);
}
