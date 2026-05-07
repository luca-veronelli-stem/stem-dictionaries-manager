using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// Bootstrap-token lookup + state-transition service for the registration
/// flow. Mint comes in US2; this implementation covers US1's
/// <see cref="LookupAsync"/> and <see cref="MarkUsedAsync"/>.
/// </summary>
public class BootstrapTokenService : IBootstrapTokenService
{
    private readonly IBootstrapTokenRepository _tokens;
    private readonly IPasswordHasher _hasher;

    public BootstrapTokenService(IBootstrapTokenRepository tokens, IPasswordHasher hasher)
    {
        _tokens = tokens;
        _hasher = hasher;
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
