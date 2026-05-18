using System.Text.Json.Serialization;

namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of the descriptor sub-object on <c>POST /register</c>. All
/// fields are nullable so the endpoint can capture missing-field cases
/// for the audit trail; semantic validation lives in
/// <c>RegistrationService</c>. <see cref="AppVersion"/>, when present,
/// must conform to SemVer 2.0 (validated at the service layer); other
/// fields are validated per the active per-<c>clientApp</c>
/// <c>DescriptorPolicy</c>. Failures share the same
/// <c>{ "error": "..." }</c> envelope across status codes.
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
