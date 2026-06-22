using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Microsoft.Extensions.Logging.Abstractions;
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
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);

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
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);

        BootstrapToken? miss = await sut.LookupAsync("stbt_unknown-token");

        Assert.Null(miss);
    }

    [Fact]
    public async Task LookupAsync_EmptyPlaintext_ReturnsNullWithoutHittingRepo()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);

        Assert.Null(await sut.LookupAsync(string.Empty));
    }

    [Theory]
    [InlineData(BootstrapTokenStatus.Issued)]
    [InlineData(BootstrapTokenStatus.Used)]
    [InlineData(BootstrapTokenStatus.Revoked)]
    public async Task LookupAsync_PlaintextMatchesAnyStatus_ReturnsDomainTokenWithThatStatus(
        BootstrapTokenStatus status)
    {
        // #58: lookup iterates across all statuses so RegistrationService can
        // branch Used/Revoked into their contracted 409/423 outcomes instead
        // of conflating them with token-unknown into the 401 path.
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        repo.Seed(new BootstrapTokenEntity
        {
            ClientApp = "ButtonPanelTester",
            SecretHash = hasher.Hash("stbt_terminal-token"),
            MintedAt = _mintedAt,
            ExpiresAt = _expiresAt,
            Status = status
        });
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);

        BootstrapToken? hit = await sut.LookupAsync("stbt_terminal-token");

        Assert.NotNull(hit);
        Assert.Equal(status, hit!.Status);
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
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);
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
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);

        // Race-loser branch: domain MarkUsed throws BootstrapTokenStateException
        // (a subtype of InvalidOperationException) carrying the FoundStatus, so
        // RegistrationService can classify the audit outcome.
        BootstrapTokenStateException ex =
            await Assert.ThrowsAsync<BootstrapTokenStateException>(
                () => sut.MarkUsedAsync(seeded.Id, installationId: 142,
                    usedAt: _mintedAt.AddHours(1)));
        Assert.Equal(BootstrapTokenStatus.Used, ex.FoundStatus);
    }

    [Fact]
    public async Task MarkUsedAsync_UnknownTokenId_Throws()
    {
        FakeBootstrapTokenRepository repo = new();
        FakePasswordHasher hasher = new();
        BootstrapTokenService sut = new(repo, hasher, NullLogger<BootstrapTokenService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.MarkUsedAsync(tokenId: 99, installationId: 1, usedAt: _mintedAt));
    }
}
