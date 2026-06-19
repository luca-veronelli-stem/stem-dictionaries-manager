namespace API.Endpoints;

/// <summary>
/// Shared OpenAPI response-type conventions for the business endpoints (#13).
/// </summary>
internal static class EndpointConventions
{
    /// <summary>
    /// Documents the two responses every authenticated business endpoint can
    /// return regardless of its own success/4xx shape: <c>401</c> from
    /// <see cref="API.Middleware.ApiKeyMiddleware"/> and <c>503</c> from
    /// <see cref="API.Middleware.DatabaseExceptionMiddleware"/>.
    /// </summary>
    internal static RouteHandlerBuilder ProducesAuthAndDbErrors(this RouteHandlerBuilder builder)
        => builder
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status503ServiceUnavailable);
}
