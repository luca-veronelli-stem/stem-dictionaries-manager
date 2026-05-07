namespace Services.Interfaces.Auth;

/// <summary>
/// CSPRNG wire-format generator for bootstrap tokens and per-installation
/// API credentials (research.md § R5). Extracted as an interface so
/// services can be tested against a deterministic fake.
/// </summary>
public interface ITokenGenerator
{
    string GenerateBootstrapToken();
    string GenerateApiCredential();
}
