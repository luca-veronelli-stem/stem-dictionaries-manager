namespace Services.Interfaces.Auth;

/// <summary>
/// PBKDF2 hash + verify surface (research.md § R3). Extracted as an interface
/// so consumers (services, validator) can be tested against a deterministic
/// fake without paying the real iteration cost.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Derives a self-describing PBKDF2 hash with a fresh CSPRNG salt.</summary>
    string Hash(string plaintext);

    /// <summary>Constant-time verification; <c>false</c> on any parse failure.</summary>
    bool Verify(string plaintext, string storedHash);
}
