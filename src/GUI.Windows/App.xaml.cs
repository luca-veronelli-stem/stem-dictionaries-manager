using System.IO;
using System.Windows;
using GUI.Windows.ViewModels;
using GUI.Windows.Views;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services;

namespace GUI.Windows;

/// <summary>
/// Entry point of the WPF application.
/// Configures the DI container and starts MainWindow.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                // WPF has no console: route diagnostics to the debugger output
                // only, dropping the default console/event providers' noise.
                logging.ClearProviders();
                logging.AddDebug();
            })
            .ConfigureServices((context, services) =>
            {
                // Reads provider and connection string from appsettings.json / User Secrets
                // Default: SqlServer (production). For development, appsettings.json overrides with "Sqlite".
                string provider = context.Configuration["DatabaseProvider"] ?? "SqlServer";
                bool useSqlServer = provider.Equals("SqlServer",
                    StringComparison.OrdinalIgnoreCase);
                string connectionString = Infrastructure.DependencyInjection.ResolveConnectionString(
                    context.Configuration.GetConnectionString(
                        useSqlServer ? "SqlServer" : "Sqlite"),
                    useSqlServer);

                // Infrastructure layer (DbContext + Repositories)
                services.AddInfrastructure(connectionString, useSqlServer);

                // Services layer (Business logic)
                services.AddServices();

                // GUI layer (ViewModels + UI Services)
                services.AddGUI();

                // MainWindow + LoginView (ViewModels are already registered by AddGUI)
                services.AddTransient<MainWindow>();
                services.AddTransient<LoginView>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        // Create/update the DB and seed initial data if empty.
        // Retry loop: when the DB is unreachable, show a Retry/Exit dialog.
        while (true)
        {
            try
            {
                using IServiceScope scope = _host.Services.CreateScope();
                AppDbContext dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                // Provider policy (see docs/Persistence.md):
                //   SQL Server -> always Migrate (versioned migrations).
                //   SQLite     -> always EnsureCreated (schema built from the
                //                 model; migrations are SQL Server-only).
                // Model-level seed data (HasData, e.g. the system-admin user)
                // is applied by both paths; migration InsertData is not.
                if (dbContext.Database.IsSqlServer())
                {
                    await dbContext.Database.MigrateAsync();
                }
                else
                {
                    await dbContext.Database.EnsureCreatedAsync();
                }

                await DatabaseSeeder.SeedAsync(dbContext);
                break;
            }
            catch (Exception ex)
            {
                bool retry = DarkDialog.ShowConfirm(
                    "Database connection error",
                    "Unable to connect to the database.\n\n"
                    + ex.Message
                    + "\n\nRetry?");

                if (!retry)
                {
                    Shutdown();
                    return;
                }
            }
        }

        // Configure MainWindow
        MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow; // Explicit: prevents a startup DarkDialog from remaining as MainWindow
        MainViewModel mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainViewModel;

        // Subscribe to the logout event to display the LoginView again
        mainViewModel.LoggedOut += () => ShowLoginView(mainViewModel);

        // Show the LoginView at startup
        ShowLoginView(mainViewModel);

        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>
    /// Shows the login view and handles the callback.
    /// </summary>
    private async void ShowLoginView(MainViewModel mainViewModel)
    {
        LoginViewModel loginViewModel = _host.Services.GetRequiredService<LoginViewModel>();
        LoginView loginView = _host.Services.GetRequiredService<LoginView>();
        loginView.DataContext = loginViewModel;

        // Clear previous subscriptions to avoid duplicate invocations
        loginViewModel.ClearSubscriptions();

        // When the user confirms the login, set the user and navigate
        loginViewModel.LoginConfirmed += user =>
        {
            mainViewModel.SetUserAndNavigate(user);
        };

        mainViewModel.CurrentViewModel = loginView;
        mainViewModel.PageTitle = "Login";

        // Load users from the database
        await loginViewModel.LoadUsersAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
