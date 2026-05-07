using Core.Enums.Auth;

namespace Core.Models.Auth;

/// <summary>
/// Per-(client app, OS user, machine) identity created by a successful
/// <c>POST /register</c>. Owns exactly one <see cref="InstallationApiCredential"/>
/// in this feature; key rotation (many-credentials-per-installation) is a
/// follow-up.
/// </summary>
public class Installation
{
    public int Id { get; private set; }
    public string ClientApp { get; private set; }
    public string OsUserId { get; private set; }
    public string MachineId { get; private set; }
    public Guid InstallGuid { get; private set; }
    public string? AppVersion { get; private set; }
    public string DescriptorJson { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public InstallationStatus Status { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public Installation(string clientApp, string osUserId, string machineId,
        Guid installGuid, string? appVersion, string descriptorJson, DateTime registeredAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientApp);
        ArgumentException.ThrowIfNullOrWhiteSpace(osUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(machineId);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptorJson);

        if (installGuid == Guid.Empty)
        {
            throw new ArgumentException("InstallGuid must be a non-zero GUID.", nameof(installGuid));
        }

        if (appVersion is not null && string.IsNullOrWhiteSpace(appVersion))
        {
            throw new ArgumentException("AppVersion, when present, must be non-empty.", nameof(appVersion));
        }

        ClientApp = clientApp;
        OsUserId = osUserId;
        MachineId = machineId;
        InstallGuid = installGuid;
        AppVersion = appVersion?.Trim();
        DescriptorJson = descriptorJson;
        RegisteredAt = registeredAt;
        Status = InstallationStatus.Active;
    }

    /// <summary>Factory method to reconstruct from the DB.</summary>
    public static Installation Restore(int id, string clientApp, string osUserId,
        string machineId, Guid installGuid, string? appVersion, string descriptorJson,
        DateTime registeredAt, InstallationStatus status, DateTime? revokedAt)
    {
        var installation = new Installation(clientApp, osUserId, machineId, installGuid,
            appVersion, descriptorJson, registeredAt)
        {
            Id = id,
            Status = status,
            RevokedAt = revokedAt
        };
        return installation;
    }
}
