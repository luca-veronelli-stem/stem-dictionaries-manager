using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Services.Auth;
using Tests.Unit.Services.Auth.Fakes;

namespace Tests.Unit.Services.Auth;

public class BootstrapTokenServiceTests
{
    private static readonly DateTime _mintedAt = new(2026, 5, 7, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime _expiresAt = _mintedAt.AddDays(30);

    [Fact]
    public async Task LookupAsync_PlaintextMatchesActiveRow_ReturnsDomainToken()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_real-token-plaintext"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = BootstrapTokenStatus.Issued
        });
        BootstrapTokenService sut = new(repo, hasher);

        BootstrapToken? hit = await sut.LookupAsync("stbt_real-token-plaintext");

        Assert.NotNull(hit);
        Assert.Equal("ButtonPanelTester", hit!.ClientApp);
        Assert.Equal(BootstrapTokenStatus.Issued, hit.Status);
    }

    [Fact]
    public async Task LookupAsync_NoMatch_ReturnsNull()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_actual-token"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = BootstrapTokenStatus.Issued
        });
        BootstrapTokenService sut = new(repo, hasher);

        BootstrapToken? miss = await sut.LookupAsync("stbt_unknown-token");

        Assert.Null(miss);
    }

    [Fact]
    public async Task LookupAsync_EmptyPlaintext_ReturnsNullWithoutHittingRepo()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        BootstrapTokenService sut = new(repo, hasher);

        Assert.Null(await sut.LookupAsync(string.Empty));
    }

    [Fact]
    public async Task LookupAsync_OnlyIteratesIssuedTokens_NotUsedOrRevoked()
    {
        // T045: iteration is bounded by ListByStatusAsync(Issued) — terminal-state
        // rows must not match even if their hash collides with the input.
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_used-already"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = BootstrapTokenStatus.Used
        });
        repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_revoked-already"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = BootstrapTokenStatus.Revoked
        });
        BootstrapTokenService sut = new(repo, hasher);

        Assert.Null(await sut.LookupAsync("stbt_used-already"));
        Assert.Null(await sut.LookupAsync("stbt_revoked-already"));
    }

    [Fact]
    public async Task MarkUsedAsync_UpdatesEntityFieldsAndDoesNotCommit()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        BootstrapTokenEntity seeded = repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_x"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = BootstrapTokenStatus.Issued
        });
        BootstrapTokenService sut = new(repo, hasher);
        DateTime usedAt = _mintedAt.AddHours(2);

        await sut.MarkUsedAsync(seeded.Id, installationId: 142, usedAt: usedAt);

        Assert.Equal(BootstrapTokenStatus.Used, seeded.Status);
        Assert.Equal(usedAt, seeded.UsedAt);
        Assert.Equal(142, seeded.ConsumedByInstallationId);
        Assert.Equal(1, repo.UpdateCallCount);
    }

    [Fact]
    public async Task MarkUsedAsync_TokenAlreadyUsed_Throws()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        BootstrapTokenEntity seeded = repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_x"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = BootstrapTokenStatus.Used,
            UsedAt = _mintedAt.AddMinutes(1),
            ConsumedByInstallationId = 99
        });
        BootstrapTokenService sut = new(repo, hasher);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.MarkUsedAsync(seeded.Id, installationId: 142, usedAt: _mintedAt.AddHours(1)));
    }

    [Fact]
    public async Task MarkUsedAsync_UnknownTokenId_Throws()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        BootstrapTokenService sut = new(repo, hasher);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.MarkUsedAsync(tokenId: 99, installationId: 1, usedAt: _mintedAt));
    }
}
