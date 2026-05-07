namespace API.Middleware;

/// <summary>
/// Middleware that handles DB exceptions (API-004).
/// Catches connection errors and returns 503 Service Unavailable with a structured JSON body.
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
                    error = "Database unavailable. Please retry in a few minutes.",
                    detail = ex.Message
                });
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Database unavailable. Please retry in a few minutes."
                });
            }
        }
    }

    /// <summary>
    /// Returns true if the exception relates to a DB connection problem.
    /// Checks the type and InnerException to cover EF Core wrappers.
    /// </summary>
    private static bool IsDatabaseException(Exception ex) =>
        ex is TimeoutException
        || ex.GetType().Name == "SqlException"
        || (ex.InnerException is not null
            && IsDatabaseException(ex.InnerException));
}
