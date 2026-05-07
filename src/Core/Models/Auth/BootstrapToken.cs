using Core.Enums.Auth;

namespace Core.Models.Auth;

/// <summary>
/// Single-use, time-bounded, client-app-scoped credential that authorises
/// exactly one <c>POST /register</c>. Persisted as a PBKDF2 hash of the
/// plaintext (per R3); plaintext is returned to the admin once at mint
/// time and never again (FR-014).
/// </summary>
public class BootstrapToken
{
    /// <summary>Lower bound for the token's lifetime (FR-007).</summary>
    public static readonly TimeSpan MinTtl = TimeSpan.FromHours(1);

    /// <summary>Upper bound for the token's lifetime (FR-007).</summary>
    public static readonly TimeSpan MaxTtl = TimeSpan.FromDays(90);

    public int Id { get; private set; }
    public string ClientApp { get; private set; }
    public string SecretHash { get; private set; }
    public DateTime MintedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public BootstrapTokenStatus Status { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public int? ConsumedByInstallationId { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public BootstrapToken(string clientApp, string secretHash,
        DateTime mintedAt, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientApp);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretHash);

        TimeSpan ttl = expiresAt - mintedAt;
        if (ttl < MinTtl || ttl > MaxTtl)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAt),
                $"BootstrapToken TTL must be within [{MinTtl}, {MaxTtl}] (FR-007); was {ttl}.");
        }

        ClientApp = clientApp;
        SecretHash = secretHash;
        MintedAt = mintedAt;
        ExpiresAt = expiresAt;
        Status = BootstrapTokenStatus.Issued;
    }

    /// <summary>Factory method to reconstruct from the DB.</summary>
    public static BootstrapToken Restore(int id, string clientApp, string secretHash,
        DateTime mintedAt, DateTime expiresAt, BootstrapTokenStatus status,
        DateTime? usedAt, int? consumedByInstallationId, DateTime? revokedAt)
    {
        var token = new BootstrapToken(clientApp, secretHash, mintedAt, expiresAt)
        {
            Id = id,
            Status = status,
            UsedAt = usedAt,
            ConsumedByInstallationId = consumedByInstallationId,
            RevokedAt = revokedAt
        };
        return token;
    }

    /// <summary>
    /// Transitions <see cref="Status"/> from <see cref="BootstrapTokenStatus.Issued"/>
    /// to <see cref="BootstrapTokenStatus.Used"/>, recording the consuming
    /// installation id (data-model invariant 1). Irreversible.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the token is not in <see cref="BootstrapTokenStatus.Issued"/> state.
    /// </exception>
    public void MarkUsed(DateTime usedAt, int installationId)
    {
        if (Status != BootstrapTokenStatus.Issued)
        {
            throw new InvalidOperationException(
                $"BootstrapToken in state {Status} cannot transition to Used.");
        }
        if (installationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installationId),
                "InstallationId must be a positive integer.");
        }

        Status = BootstrapTokenStatus.Used;
        UsedAt = usedAt;
        ConsumedByInstallationId = installationId;
    }

    /// <summary>
    /// Transitions <see cref="Status"/> from <see cref="BootstrapTokenStatus.Issued"/>
    /// to <see cref="BootstrapTokenStatus.Revoked"/>. Irreversible.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the token is not in <see cref="BootstrapTokenStatus.Issued"/> state.
    /// </exception>
    public void Revoke(DateTime revokedAt)
    {
        if (Status != BootstrapTokenStatus.Issued)
        {
            throw new InvalidOperationException(
                $"BootstrapToken in state {Status} cannot transition to Revoked.");
        }

        Status = BootstrapTokenStatus.Revoked;
        RevokedAt = revokedAt;
    }

    /// <summary>
    /// Returns <c>true</c> when the token is logically expired —
    /// <c>Status = Issued &amp;&amp; now &gt; ExpiresAt</c>. Per
    /// <c>data-model.md</c>, <see cref="BootstrapTokenStatus.Expired"/> is a
    /// derived state evaluated at read time and is never persisted.
    /// </summary>
    public bool IsExpiredAt(DateTime now)
        => Status == BootstrapTokenStatus.Issued && now > ExpiresAt;
}
