using Core.Enums.Auth;
using Core.Models.Auth;

namespace Tests.Unit.Models.Auth;

/// <summary>
/// Pure-domain transition matrix per <c>data-model.md</c> § BootstrapToken
/// state machine. Mirrors the future Lean preservation theorems flagged in
/// <c>plan.md</c> § Constitution Check Principle III TODO(LEAN_WORKSPACE).
/// </summary>
public class BootstrapTokenStateMachineTests
{
    private static BootstrapToken NewIssuedToken(DateTime? mintedAt = null,
        TimeSpan? ttl = null, string clientApp = "ButtonPanelTester",
        string secretHash = "pbkdf2-sha256$600000$AAAAAAAAAAAAAAAAAAAAAA==$" +
            "B2GZ8/g6oW5jaeATsnPVQOyKfV7gcRmkxh7K6OjA4ho=")
    {
        DateTime minted = mintedAt ?? new DateTime(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);
        TimeSpan span = ttl ?? TimeSpan.FromDays(30);
        return new BootstrapToken(clientApp, secretHash, minted, minted + span);
    }

    [Fact]
    public void NewToken_StartsInIssuedState()
    {
        BootstrapToken token = NewIssuedToken();

        Assert.Equal(BootstrapTokenStatus.Issued, token.Status);
        Assert.Null(token.UsedAt);
        Assert.Null(token.RevokedAt);
        Assert.Null(token.ConsumedByInstallationId);
    }

    [Fact]
    public void MarkUsed_FromIssued_TransitionsToUsedAndRecordsConsumer()
    {
        BootstrapToken token = NewIssuedToken();
        DateTime usedAt = new(2026, 5, 8, 9, 0, 0, DateTimeKind.Utc);

        token.MarkUsed(usedAt, installationId: 142);

        Assert.Equal(BootstrapTokenStatus.Used, token.Status);
        Assert.Equal(usedAt, token.UsedAt);
        Assert.Equal(142, token.ConsumedByInstallationId);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void MarkUsed_FromUsed_Throws()
    {
        BootstrapToken token = NewIssuedToken();
        token.MarkUsed(DateTime.UtcNow, installationId: 1);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => token.MarkUsed(DateTime.UtcNow, installationId: 2));

        Assert.Contains("Used", ex.Message);
    }

    [Fact]
    public void MarkUsed_FromRevoked_Throws()
    {
        BootstrapToken token = NewIssuedToken();
        token.Revoke(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => token.MarkUsed(DateTime.UtcNow, installationId: 1));
    }

    [Fact]
    public void MarkUsed_WithNonPositiveInstallationId_Throws()
    {
        BootstrapToken token = NewIssuedToken();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => token.MarkUsed(DateTime.UtcNow, installationId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => token.MarkUsed(DateTime.UtcNow, installationId: -3));
    }

    [Fact]
    public void Revoke_FromIssued_TransitionsToRevokedAndStampsRevokedAt()
    {
        BootstrapToken token = NewIssuedToken();
        DateTime revokedAt = new(2026, 5, 8, 9, 0, 0, DateTimeKind.Utc);

        token.Revoke(revokedAt);

        Assert.Equal(BootstrapTokenStatus.Revoked, token.Status);
        Assert.Equal(revokedAt, token.RevokedAt);
        Assert.Null(token.UsedAt);
        Assert.Null(token.ConsumedByInstallationId);
    }

    [Fact]
    public void Revoke_FromUsed_Throws()
    {
        BootstrapToken token = NewIssuedToken();
        token.MarkUsed(DateTime.UtcNow, installationId: 1);

        Assert.Throws<InvalidOperationException>(
            () => token.Revoke(DateTime.UtcNow));
    }

    [Fact]
    public void Revoke_FromRevoked_Throws()
    {
        BootstrapToken token = NewIssuedToken();
        token.Revoke(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(
            () => token.Revoke(DateTime.UtcNow));
    }

    [Fact]
    public void IsExpiredAt_OnlyTrueForIssuedTokensPastTheirExpiry()
    {
        DateTime mintedAt = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);
        BootstrapToken token = NewIssuedToken(mintedAt, ttl: TimeSpan.FromHours(1));
        DateTime atExpiry = mintedAt.AddHours(1);
        DateTime pastExpiry = mintedAt.AddHours(1).AddSeconds(1);
        DateTime beforeExpiry = mintedAt.AddMinutes(30);

        Assert.False(token.IsExpiredAt(beforeExpiry));
        Assert.False(token.IsExpiredAt(atExpiry), "expiry boundary is exclusive");
        Assert.True(token.IsExpiredAt(pastExpiry));
    }

    [Fact]
    public void IsExpiredAt_NeverTrueAfterTransitioningOutOfIssued()
    {
        DateTime mintedAt = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);
        BootstrapToken used = NewIssuedToken(mintedAt, ttl: TimeSpan.FromHours(1));
        used.MarkUsed(mintedAt.AddMinutes(10), installationId: 1);

        BootstrapToken revoked = NewIssuedToken(mintedAt, ttl: TimeSpan.FromHours(1));
        revoked.Revoke(mintedAt.AddMinutes(20));

        DateTime longAfterExpiry = mintedAt.AddDays(1);
        Assert.False(used.IsExpiredAt(longAfterExpiry),
            "Used is terminal — Expired is not derivable once a transition has happened");
        Assert.False(revoked.IsExpiredAt(longAfterExpiry),
            "Revoked is terminal — Expired is not derivable once a transition has happened");
    }

    [Fact]
    public void Constructor_RejectsTtlBelow1Hour()
    {
        DateTime mintedAt = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BootstrapToken("ButtonPanelTester", "hash",
                mintedAt, mintedAt.AddMinutes(59)));
    }

    [Fact]
    public void Constructor_RejectsTtlAbove90Days()
    {
        DateTime mintedAt = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BootstrapToken("ButtonPanelTester", "hash",
                mintedAt, mintedAt.AddDays(90).AddSeconds(1)));
    }
}
