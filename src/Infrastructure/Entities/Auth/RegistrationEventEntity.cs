using Core.Enums.Auth;

namespace Infrastructure.Entities.Auth;

public class RegistrationEventEntity
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? ClaimedClientApp { get; set; }
    public string? ClaimedOsUserId { get; set; }
    public string? ClaimedMachineId { get; set; }
    public Guid? ClaimedInstallGuid { get; set; }
    public string? ClaimedAppVersion { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string? DescriptorJson { get; set; }
    public RegistrationOutcome Outcome { get; set; }
    public int? ResultingInstallationId { get; set; }

    // Navigation (optional — only set on Outcome = Success)
    public InstallationEntity? ResultingInstallation { get; set; }
}
