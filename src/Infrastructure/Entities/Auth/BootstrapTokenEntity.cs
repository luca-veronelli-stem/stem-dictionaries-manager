using Core.Enums.Auth;

namespace Infrastructure.Entities.Auth;

public class BootstrapTokenEntity
{
    public int Id { get; set; }
    public string ClientApp { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public DateTime MintedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public BootstrapTokenStatus Status { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? ConsumedByInstallationId { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Navigation (optional — only populated when Status = Used)
    public InstallationEntity? ConsumedByInstallation { get; set; }
}
