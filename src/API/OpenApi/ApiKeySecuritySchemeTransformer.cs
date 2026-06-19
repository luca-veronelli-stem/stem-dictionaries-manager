using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace API.OpenApi;

/// <summary>
/// Adds the <c>X-Api-Key</c> API-key security scheme to the generated OpenAPI
/// document and applies it as a document-wide requirement, so the Swagger UI
/// renders an "Authorize" button and sends the header on try-it-out calls
/// (#12). The Microsoft.OpenApi v2 surface shipped with .NET 10 replaced the
/// inline SecurityScheme wiring used previously; a document transformer is the
/// supported seam, and the requirement key is an
/// <see cref="OpenApiSecuritySchemeReference"/> pointing at the registered
/// scheme rather than the scheme instance itself.
/// </summary>
internal sealed class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    internal const string SchemeId = "ApiKey";
    private const string HeaderName = "X-Api-Key";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = HeaderName,
            Description =
                "Configured or per-installation API key, sent in the X-Api-Key header."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = scheme;

        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
        };
        (document.Security ??= []).Add(requirement);

        return Task.CompletedTask;
    }
}
