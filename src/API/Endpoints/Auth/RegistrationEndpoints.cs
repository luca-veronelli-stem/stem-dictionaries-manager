using System.Text.Json;
using API.Dtos.Auth;
using Core.Enums.Auth;
using Microsoft.Extensions.Logging;
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
    /// <summary>
    /// Shared failure body — same <c>{ "error": "..." }</c> envelope across
    /// all failure statuses. The status code distinguishes the failure
    /// class (per the narrowed FR-002); the body stays a single string so
    /// clients cannot use body inspection as a token-validity oracle for
    /// the three scope-related 401 modes.
    /// </summary>
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
        IRegistrationService registration,
        ILoggerFactory loggerFactory, CancellationToken ct)
    {
        ILogger logger = loggerFactory.CreateLogger("API.Endpoints.Auth.RegistrationEndpoints");
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
                RegistrationResult.Failure failure => RawJson(FailureBody, StatusFor(failure.Outcome)),
                _ => RawJson(FailureBody, StatusCodes.Status401Unauthorized)
            };
        }
        catch (Exception ex)
        {
            // FR-013: audit/persistence failures must not look like a token
            // failure to the client — return 500, never 401.
            // FR-008 (#71): log the exception object at error level before
            // returning so the proximate cause surfaces in the application
            // log alongside the 500. Without this, operators must enable
            // EF verbose logging to find out what threw.
            logger.LogError(ex,
                "Registration failed with unhandled exception (sourceIp={SourceIp}, clientApp={ClientApp}, installGuid={InstallGuid}).",
                sourceIp, dto?.Descriptor?.ClientApp, dto?.Descriptor?.InstallGuid);
            return RawJson(AuditFailureBody, StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Maps a <see cref="RegistrationOutcome"/> to the wire status code
    /// per <c>contracts/register.md</c>. The three scope-related outcomes
    /// (<see cref="RegistrationOutcome.TokenInvalid"/>,
    /// <see cref="RegistrationOutcome.ClientScopeMismatch"/>) collapse to
    /// 401 to hide token-scope information; every other outcome gets its
    /// own RFC-meaningful status.
    /// </summary>
    private static int StatusFor(RegistrationOutcome outcome) => outcome switch
    {
        RegistrationOutcome.TokenMissing => StatusCodes.Status400BadRequest,
        RegistrationOutcome.DescriptorMalformed => StatusCodes.Status400BadRequest,
        RegistrationOutcome.DescriptorMissingField => StatusCodes.Status400BadRequest,
        RegistrationOutcome.InstallGuidInvalid => StatusCodes.Status400BadRequest,
        RegistrationOutcome.TokenInvalid => StatusCodes.Status401Unauthorized,
        RegistrationOutcome.ClientScopeMismatch => StatusCodes.Status401Unauthorized,
        RegistrationOutcome.TokenAlreadyUsed => StatusCodes.Status409Conflict,
        RegistrationOutcome.TokenExpired => StatusCodes.Status410Gone,
        RegistrationOutcome.TokenRevoked => StatusCodes.Status423Locked,
        _ => StatusCodes.Status401Unauthorized
    };

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
