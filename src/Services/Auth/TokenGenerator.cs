using System.Security.Cryptography;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// CSPRNG-backed wire-format generator for bootstrap tokens and
/// per-installation API credentials per research.md § R5. Output shape:
/// <c>&lt;prefix&gt;_&lt;43 base64url chars, no padding&gt;</c>.
/// </summary>
public class TokenGenerator : ITokenGenerator
{
    private const string BootstrapPrefix = "stbt_";
    private const string CredentialPrefix = "stak_";
    private const int RandomBytes = 32;

    /// <summary>Generates a fresh bootstrap-token plaintext (<c>stbt_…</c>).</summary>
    public string GenerateBootstrapToken() => BootstrapPrefix + GenerateBase64UrlSecret();

    /// <summary>Generates a fresh installation-credential plaintext (<c>stak_…</c>).</summary>
    public string GenerateApiCredential() => CredentialPrefix + GenerateBase64UrlSecret();

    private static string GenerateBase64UrlSecret()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(RandomBytes);
        return Base64Url(bytes);
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes)
    {
        // Standard base64, then URL-safe substitutions and padding strip.
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
