using Services.Auth;

namespace Tests.Unit.Services.Auth;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_ThenVerify_WithSamePlaintext_ReturnsTrue()
    {
        const string plaintext = "stbt_abcdefghijklmnopqrstuvwxyz0123456789ABCDEF";

        string hash = _sut.Hash(plaintext);

        Assert.True(_sut.Verify(plaintext, hash));
    }

    [Fact]
    public void Verify_WithWrongPlaintext_ReturnsFalse()
    {
        const string plaintext = "stbt_correct-plaintext-43-chars-of-base64url-padding";
        const string wrong = "stbt_almost-the-same-but-not-quite-43-chars-of-padding!";

        string hash = _sut.Hash(plaintext);

        Assert.False(_sut.Verify(wrong, hash));
    }

    [Fact]
    public void Hash_ProducesDistinctOutputsForSamePlaintext_BecauseSaltIsRandom()
    {
        const string plaintext = "stak_repeated-input-should-produce-distinct-hashes";

        string hash1 = _sut.Hash(plaintext);
        string hash2 = _sut.Hash(plaintext);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_FormatIsSelfDescribingPbkdf2Sha256()
    {
        // R3: pbkdf2-sha256$<iterations>$<salt-b64>$<hash-b64>
        const string plaintext = "any-plaintext-will-do";

        string hash = _sut.Hash(plaintext);

        string[] parts = hash.Split('$');
        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2-sha256", parts[0]);
        Assert.True(int.TryParse(parts[1], out int iterations));
        Assert.Equal(600_000, iterations);
        // 16-byte salt, base64 (with padding) is 24 chars; without padding is 22.
        // Implementation may emit either; both are accepted by the verifier.
        Assert.True(parts[2].Length is 22 or 24, $"Unexpected salt length: {parts[2].Length}");
        // 32-byte hash, base64 is 44 (padded) or 43 (unpadded).
        Assert.True(parts[3].Length is 43 or 44, $"Unexpected hash length: {parts[3].Length}");
    }

    [Fact]
    public void Verify_WithMalformedHashString_ReturnsFalse()
    {
        // Defensive: a verifier that early-returns on parse failure must
        // still return false (not throw) — keeps the unified-401 invariant
        // intact even in the face of corrupted DB rows.
        Assert.False(_sut.Verify("anything", "not-a-real-hash"));
        Assert.False(_sut.Verify("anything", "pbkdf2-sha256$600000$only-three-parts"));
        Assert.False(_sut.Verify("anything", "pbkdf2-sha512$600000$AAAAAAAAAAAAAAAAAAAAAA==$" +
            "B".PadRight(43, 'B')));
    }

    [Fact]
    public void Verify_AcrossManyWrongPlaintexts_NeverThrows()
    {
        // Exercises the FixedTimeEquals path: every comparison goes the
        // full length, regardless of where the bytes diverge.
        const string plaintext = "stak_baseline-secret-for-fixed-time-comparison";
        string hash = _sut.Hash(plaintext);

        for (int i = 0; i < 32; i++)
        {
            string mutated = plaintext[..i] + 'X' + plaintext[(i + 1)..];
            Assert.False(_sut.Verify(mutated, hash));
        }
    }
}
