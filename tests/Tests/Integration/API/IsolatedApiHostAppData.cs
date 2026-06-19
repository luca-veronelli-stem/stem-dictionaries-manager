using System.Runtime.CompilerServices;

namespace Tests.Integration.API;

/// <summary>
/// Points <c>ConnectionStrings__Sqlite</c> at a throwaway temp path before any
/// <c>WebApplicationFactory&lt;Program&gt;</c> boots the real <c>Program.cs</c>.
/// <para>
/// Program.cs resolves the SQLite connection string from configuration at
/// <c>WebApplication.CreateBuilder</c> time -- before any test-side
/// <c>ConfigureAppConfiguration</c> hook can flow under minimal hosting -- so an
/// environment variable (read by the builder's default
/// <c>AddEnvironmentVariables</c>) is the only seam that lands in time. Setting
/// it keeps every API-host test off the default
/// <c>%LocalAppData%\Stem\DictionariesManager</c> path, so none runs
/// <c>StemAppDataMigration.MigrateOnce</c> against the machine-global
/// <c>.appdata-version</c> marker that parallel hosts otherwise race on (#113).
/// </para>
/// </summary>
internal static class IsolatedApiHostAppData
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Each factory swaps the real DbContext for an in-memory
        // SqliteConnection, so this file is never created -- a non-empty,
        // process-unique path is all the default-path branch in
        // Infrastructure.DependencyInjection.ResolveConnectionString needs to
        // be bypassed.
        string isolated = Path.Combine(
            Path.GetTempPath(), $"stem-dm-apitests-{Guid.NewGuid():N}.db");
        Environment.SetEnvironmentVariable("ConnectionStrings__Sqlite", isolated);
    }
}
