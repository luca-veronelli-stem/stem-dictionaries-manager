using System.Security.Cryptography;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// PBKDF2-HMAC-SHA256 password hasher per research.md § R3.
/// Used for both bootstrap tokens and per-installation API credentials —
/// the plaintext is server-minted CSPRNG output (R5), so the iteration
/// count is a defence-in-depth hedge against DB-leak + weak-RNG combos
/// rather than offline-dictionary protection.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    /// <summary>
    /// Derives a self-describing PBKDF2 hash of <paramref name="plaintext"/>.
    /// Format: <c>pbkdf2-sha256$&lt;iterations&gt;$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>.
    /// Each call uses a fresh CSPRNG salt — repeated calls produce distinct outputs.
    /// </summary>
    public string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password: plaintext,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: HashBytes);

        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Constant-time verification of <paramref name="plaintext"/> against
    /// <paramref name="storedHash"/>. Returns <c>false</c> on any parse
    /// failure; never throws on malformed input.
    /// </summary>
    public bool Verify(string plaintext, string storedHash)
    {
        if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        string[] parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Algorithm)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out int iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] candidate = Rfc2898DeriveBytes.Pbkdf2(
            password: plaintext,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: expected.Length);

        return CryptographicOperations.FixedTimeEquals(candidate, expected);
    }
}
