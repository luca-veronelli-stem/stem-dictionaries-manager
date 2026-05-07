using System.Text.RegularExpressions;
using Services.Auth;

namespace Tests.Unit.Services.Auth;

public class TokenGeneratorTests
{
    private readonly TokenGenerator _sut = new();

    [Fact]
    public void GenerateBootstrapToken_HasStbtPrefixAnd43Base64UrlChars()
    {
        string token = _sut.GenerateBootstrapToken();

        Assert.Matches(new Regex("^stbt_[A-Za-z0-9_-]{43}$"), token);
        Assert.DoesNotContain('=', token);
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
    }

    [Fact]
    public void GenerateApiCredential_HasStakPrefixAnd43Base64UrlChars()
    {
        string credential = _sut.GenerateApiCredential();

        Assert.Matches(new Regex("^stak_[A-Za-z0-9_-]{43}$"), credential);
        Assert.DoesNotContain('=', credential);
        Assert.DoesNotContain('+', credential);
        Assert.DoesNotContain('/', credential);
    }

    [Fact]
    public void GenerateBootstrapToken_TwoCallsReturnDistinctValues()
    {
        // Entropy smoke test — 32 bytes of CSPRNG should never collide
        // in two consecutive calls under any sane RNG.
        string a = _sut.GenerateBootstrapToken();
        string b = _sut.GenerateBootstrapToken();

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GenerateApiCredential_TwoCallsReturnDistinctValues()
    {
        string a = _sut.GenerateApiCredential();
        string b = _sut.GenerateApiCredential();

        Assert.NotEqual(a, b);
    }
}
