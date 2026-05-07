using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// Issues per-installation API credentials. The plaintext is generated
/// here, hashed via <see cref="IPasswordHasher"/>, and returned once to
/// the caller; only the hash is persisted (<c>data-model.md</c> invariant
/// 4 — plaintext-once).
/// </summary>
public class InstallationCredentialService : IInstallationCredentialService
{
    private readonly IInstallationApiCredentialRepository _credentials;
    private readonly ITokenGenerator _tokens;
    private readonly IPasswordHasher _hasher;

    public InstallationCredentialService(
        IInstallationApiCredentialRepository credentials,
        ITokenGenerator tokens,
        IPasswordHasher hasher)
    {
        _credentials = credentials;
        _tokens = tokens;
        _hasher = hasher;
    }

    public async Task<(InstallationApiCredential Record, string Plaintext)> IssueAsync(
        int installationId, DateTime issuedAt, CancellationToken ct = default)
    {
        if (installationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installationId),
                "InstallationId must be a positive integer.");
        }

        string plaintext = _tokens.GenerateApiCredential();
        string hash = _hasher.Hash(plaintext);

        InstallationApiCredentialEntity entity = new()
        {
            InstallationId = installationId,
            SecretHash = hash,
            IssuedAt = issuedAt,
            Status = InstallationStatus.Active
        };

        await _credentials.AddAsync(entity, ct).ConfigureAwait(false);

        InstallationApiCredential record = new(installationId, hash, issuedAt);
        return (record, plaintext);
    }
}
