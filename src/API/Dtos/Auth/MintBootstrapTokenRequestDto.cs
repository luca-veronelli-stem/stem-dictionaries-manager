namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of <c>POST /api/admin/bootstrap-tokens</c> request body.
/// See <c>contracts/admin-bootstrap-tokens.md</c>. <see cref="TtlHours"/>
/// is optional and defaults to 720 (30 days) when omitted; the bounds
/// [1, 2160] are enforced by the service (FR-007).
/// </summary>
public sealed class MintBootstrapTokenRequestDto
{
    public string? ClientApp { get; set; }
    public int? TtlHours { get; set; }
}
