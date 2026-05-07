namespace API.Middleware;

/// <summary>
/// Middleware per gestione eccezioni DB (API-004).
/// Cattura errori di connessione e ritorna 503 Service Unavailable con JSON strutturato.
/// </summary>
public class DatabaseExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public DatabaseExceptionMiddleware(
        RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsDatabaseException(ex))
        {
            context.Response.StatusCode =
                StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";

            if (_isDevelopment)
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Database non raggiungibile. Riprovare tra qualche minuto.",
                    detail = ex.Message
                });
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Database non raggiungibile. Riprovare tra qualche minuto."
                });
            }
        }
    }

    /// <summary>
    /// Verifica se l'eccezione è relativa a un problema di connessione DB.
    /// Controlla tipo e InnerException per coprire i wrapper EF Core.
    /// </summary>
    private static bool IsDatabaseException(Exception ex) =>
        ex is TimeoutException
        || ex.GetType().Name == "SqlException"
        || (ex.InnerException is not null
            && IsDatabaseException(ex.InnerException));
}
