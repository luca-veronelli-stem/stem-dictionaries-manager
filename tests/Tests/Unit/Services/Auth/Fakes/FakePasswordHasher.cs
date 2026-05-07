using Services.Interfaces.Auth;

namespace Tests.Unit.Services.Auth.Fakes;

/// <summary>
/// Manual fake for <see cref="IPasswordHasher"/> — produces a deterministic,
/// non-cryptographic hash so tests run fast without paying PBKDF2's
/// 50 ms-per-call cost. The "hash" is just the plaintext wrapped in a
/// recognisable prefix so production code paths that look at the hash
/// shape (e.g. format checks) still see something well-formed.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "fake:";

    public string Hash(string plaintext) => Prefix + plaintext;

    public bool Verify(string plaintext, string storedHash)
        => storedHash == Prefix + plaintext;
}
