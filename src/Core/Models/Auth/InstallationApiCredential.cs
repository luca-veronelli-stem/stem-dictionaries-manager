using Core.Enums.Auth;

namespace Core.Models.Auth;

/// <summary>
/// Long-lived authentication secret bound 1:1 to an <see cref="Installation"/>.
/// Stored as a PBKDF2 hash (R3); plaintext is returned to the client exactly
/// once at registration time and never again.
/// </summary>
public class InstallationApiCredential
{
    public int Id { get; private set; }
    public int InstallationId { get; private set; }
    public string SecretHash { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public InstallationStatus Status { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public InstallationApiCredential(int installationId, string secretHash, DateTime issuedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretHash);
        if (installationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installationId),
                "InstallationId must be a positive integer.");
        }

        InstallationId = installationId;
        SecretHash = secretHash;
        IssuedAt = issuedAt;
        Status = InstallationStatus.Active;
    }

    /// <summary>Factory method to reconstruct from the DB.</summary>
    public static InstallationApiCredential Restore(int id, int installationId,
        string secretHash, DateTime issuedAt, InstallationStatus status, DateTime? revokedAt)
    {
        var credential = new InstallationApiCredential(installationId, secretHash, issuedAt)
        {
            Id = id,
            Status = status,
            RevokedAt = revokedAt
        };
        return credential;
    }
}
