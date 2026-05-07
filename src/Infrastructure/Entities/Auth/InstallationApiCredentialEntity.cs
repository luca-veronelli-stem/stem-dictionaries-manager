using Core.Enums.Auth;

namespace Infrastructure.Entities.Auth;

public class InstallationApiCredentialEntity
{
    public int Id { get; set; }
    public int InstallationId { get; set; }
    public string SecretHash { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public InstallationStatus Status { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Navigation
    public InstallationEntity Installation { get; set; } = null!;
}
