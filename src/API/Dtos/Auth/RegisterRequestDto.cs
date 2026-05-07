namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of the <c>POST /register</c> request body. See
/// <c>contracts/register.md</c>.
/// </summary>
public sealed class RegisterRequestDto
{
    public string? BootstrapToken { get; set; }
    public InstallationDescriptorDto? Descriptor { get; set; }
}
