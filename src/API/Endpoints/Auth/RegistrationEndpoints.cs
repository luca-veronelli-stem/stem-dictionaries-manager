using System.Text.Json;
using API.Dtos.Auth;
using Services.Interfaces.Auth;

namespace API.Endpoints.Auth;

/// <summary>
/// <c>POST /register</c> — bootstrap registration entry point per
/// <c>specs/001-bootstrap-registration/contracts/register.md</c>.
/// Public (unauthenticated): the <c>ApiKeyMiddleware</c> allow-list lets
/// the request through; this endpoint is what *establishes* authentication
/// for a new installation.
/// </summary>
public static class RegistrationEndpoints
{
    /// <summary>Unified failure body — FR-002, byte-identical for every 401 case.</summary>
    private const string FailureBody = "{\"error\":\"registration failed\"}";

    /// <summary>500 body for FR-013 audit-or-no-issue: server failed, not the token.</summary>
    private const string AuditFailureBody = "{\"error\":\"audit failure\"}";

    public static void MapRegistrationEndpoints(this WebApplication app)
    {
        app.MapPost("/register", Register)
            .WithName("Register")
            .WithTags("Auth")
            .AllowAnonymous();
    }

    private static async Task<IResult> Register(HttpContext context,
        IRegistrationService registration, CancellationToken ct)
    {
        // Drain the body once so we keep the raw descriptor JSON for the audit
        // trail regardless of parse outcome (DescriptorMalformed needs the
        // claimed-fields-as-submitted on the RegistrationEvent row).
        using StreamReader reader = new(context.Request.Body);
        string rawBody = await reader.ReadToEndAsync(ct);

        RegisterRequestDto? dto = TryParse(rawBody);
        string? descriptorJson = ExtractDescriptorJson(dto);
        string sourceIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        RegisterRequest request = new(
            BootstrapTokenPlaintext: dto?.BootstrapToken,
            ClientApp: dto?.Descriptor?.ClientApp,
            OsUserId: dto?.Descriptor?.OsUserId,
            MachineId: dto?.Descriptor?.MachineId,
            InstallGuid: dto?.Descriptor?.InstallGuid,
            AppVersion: dto?.Descriptor?.AppVersion,
            DescriptorJson: descriptorJson,
            SourceIp: sourceIp);

        try
        {
            RegistrationResult result = await registration.RegisterAsync(request, ct);
            return result switch
            {
                RegistrationResult.Success success => Results.Ok(new RegisterResponseDto(
                    InstallationId: success.InstallationId,
                    ApiCredential: success.ApiCredentialPlaintext,
                    IssuedAt: success.IssuedAt)),
                _ => RawJson(FailureBody, StatusCodes.Status401Unauthorized)
            };
        }
        catch
        {
            // FR-013: audit/persistence failures must not look like a token
            // failure to the client — return 500, never 401.
            return RawJson(AuditFailureBody, StatusCodes.Status500InternalServerError);
        }
    }

    private static RegisterRequestDto? TryParse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }
        try
        {
            return JsonSerializer.Deserialize<RegisterRequestDto>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractDescriptorJson(RegisterRequestDto? dto)
    {
        if (dto?.Descriptor is null)
        {
            return null;
        }
        return JsonSerializer.Serialize(dto.Descriptor);
    }

    /// <summary>
    /// Writes a literal JSON string with the exact bytes the contract
    /// specifies — no naming-policy or null-omission massaging. Required
    /// for SC-002 (byte-identical failure responses).
    /// </summary>
    private static IResult RawJson(string json, int statusCode)
        => Results.Content(json, contentType: "application/json", statusCode: statusCode);
}
