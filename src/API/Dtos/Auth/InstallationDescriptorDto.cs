using System.Text.Json.Serialization;

namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of the descriptor sub-object on <c>POST /register</c>. All
/// fields are nullable so the endpoint can capture missing-field cases
/// for the audit trail; semantic validation lives in
/// <c>RegistrationService</c> (FR-002 unified-401 — every failure mode
/// returns the same body).
/// </summary>
public sealed class InstallationDescriptorDto
{
    public string? ClientApp { get; set; }
    public string? OsUserId { get; set; }
    public string? MachineId { get; set; }
    public Guid? InstallGuid { get; set; }
    public string? AppVersion { get; set; }

    /// <summary>
    /// Forward-compatible passthrough — additional descriptor properties are
    /// accepted, persisted into the audit's <c>descriptorJson</c> blob, and
    /// ignored for validation per <c>contracts/register.md</c>.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }
}
