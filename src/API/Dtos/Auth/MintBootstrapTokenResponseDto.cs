namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of the <c>POST /api/admin/bootstrap-tokens</c> success
/// response. See <c>contracts/admin-bootstrap-tokens.md</c>. The
/// <see cref="Plaintext"/> value is returned exactly once here and is
/// never recoverable from any subsequent admin call (FR-014).
/// </summary>
public sealed record MintBootstrapTokenResponseDto(
    int TokenId,
    string ClientApp,
    string Plaintext,
    DateTime MintedAt,
    DateTime ExpiresAt);
