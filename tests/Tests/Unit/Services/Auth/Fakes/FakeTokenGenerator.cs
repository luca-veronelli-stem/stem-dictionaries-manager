using Services.Interfaces.Auth;

namespace Tests.Unit.Services.Auth.Fakes;

/// <summary>
/// Manual fake for <see cref="ITokenGenerator"/>. Returns a deterministic
/// counter-suffixed string per call so tests can assert exact emitted
/// values without paying CSPRNG cost.
/// </summary>
internal sealed class FakeTokenGenerator : ITokenGenerator
{
    private int _bootstrapCounter;
    private int _credentialCounter;

    public string GenerateBootstrapToken()
        => $"stbt_fake-bootstrap-token-{++_bootstrapCounter:D2}";

    public string GenerateApiCredential()
        => $"stak_fake-api-credential-{++_credentialCounter:D2}";
}
