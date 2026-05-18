using Core.Enums.Auth;

namespace Infrastructure.Entities.Auth;

public class InstallationEntity
{
    public int Id { get; set; }
    public string ClientApp { get; set; } = string.Empty;
    public string? OsUserId { get; set; }
    public string? MachineId { get; set; }
    public Guid InstallGuid { get; set; }
    public string? AppVersion { get; set; }
    public string DescriptorJson { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public InstallationStatus Status { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Navigation
    public InstallationApiCredentialEntity? Credential { get; set; }
}
