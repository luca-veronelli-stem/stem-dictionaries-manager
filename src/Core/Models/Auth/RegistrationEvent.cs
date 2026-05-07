using Core.Enums.Auth;

namespace Core.Models.Auth;

/// <summary>
/// Audit-trail record of one <c>POST /register</c> attempt (success or
/// failure). Lives in a dedicated <c>RegistrationEvents</c> table
/// because the path is pre-authentication and there is no current user
/// to attribute the event to (see research.md § R2).
/// </summary>
public class RegistrationEvent
{
    public int Id { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? ClaimedClientApp { get; private set; }
    public string? ClaimedOsUserId { get; private set; }
    public string? ClaimedMachineId { get; private set; }
    public Guid? ClaimedInstallGuid { get; private set; }
    public string? ClaimedAppVersion { get; private set; }
    public string SourceIp { get; private set; }
    public string? DescriptorJson { get; private set; }
    public RegistrationOutcome Outcome { get; private set; }
    public int? ResultingInstallationId { get; private set; }

    public RegistrationEvent(DateTime occurredAt, string? claimedClientApp,
        string? claimedOsUserId, string? claimedMachineId, Guid? claimedInstallGuid,
        string? claimedAppVersion, string sourceIp, string? descriptorJson,
        RegistrationOutcome outcome, int? resultingInstallationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceIp);
        if (outcome == RegistrationOutcome.Success && resultingInstallationId is null)
        {
            throw new ArgumentException(
                "ResultingInstallationId is required when Outcome = Success.",
                nameof(resultingInstallationId));
        }

        OccurredAt = occurredAt;
        ClaimedClientApp = claimedClientApp;
        ClaimedOsUserId = claimedOsUserId;
        ClaimedMachineId = claimedMachineId;
        ClaimedInstallGuid = claimedInstallGuid;
        ClaimedAppVersion = claimedAppVersion;
        SourceIp = sourceIp;
        DescriptorJson = descriptorJson;
        Outcome = outcome;
        ResultingInstallationId = resultingInstallationId;
    }

    /// <summary>Factory method to reconstruct from the DB.</summary>
    public static RegistrationEvent Restore(int id, DateTime occurredAt,
        string? claimedClientApp, string? claimedOsUserId, string? claimedMachineId,
        Guid? claimedInstallGuid, string? claimedAppVersion, string sourceIp,
        string? descriptorJson, RegistrationOutcome outcome, int? resultingInstallationId)
    {
        var evt = new RegistrationEvent(occurredAt, claimedClientApp, claimedOsUserId,
            claimedMachineId, claimedInstallGuid, claimedAppVersion, sourceIp,
            descriptorJson, outcome, resultingInstallationId)
        {
            Id = id
        };
        return evt;
    }
}
