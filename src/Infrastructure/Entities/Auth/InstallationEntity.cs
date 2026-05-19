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

    // Navigation — multi-row per Installation over the installation's
    // lifetime (spec 002): at most one Active, zero-or-more Revoked
    // historical rows. The at-most-one-Active invariant is enforced by a
    // filtered unique index on InstallationId WHERE Status = Active.
    public ICollection<InstallationApiCredentialEntity> Credentials { get; set; }
        = new List<InstallationApiCredentialEntity>();
}
