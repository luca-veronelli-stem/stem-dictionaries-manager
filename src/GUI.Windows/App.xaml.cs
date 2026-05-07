using GUI.Windows.ViewModels;
using GUI.Windows.Views;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using System.IO;
using System.Windows;

namespace GUI.Windows;

/// <summary>
/// Entry point dell'applicazione WPF.
/// Configura il DI container e avvia la MainWindow.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    /// <summary>
    /// Service provider per accesso ai servizi registrati.
    /// Usato dalle Views per creare dialog con DI.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Legge provider e connection string da appsettings.json / User Secrets
                // Default: SqlServer (produzione). Per sviluppo, appsettings.json sovrascrive con "Sqlite".
                var provider = context.Configuration["DatabaseProvider"] ?? "SqlServer";
                var useSqlServer = provider.Equals("SqlServer",
                    StringComparison.OrdinalIgnoreCase);
                var connectionString = Infrastructure.DependencyInjection.ResolveConnectionString(
                    context.Configuration.GetConnectionString(
                        useSqlServer ? "SqlServer" : "Sqlite"),
                    useSqlServer);

                // Infrastructure layer (DbContext + Repositories)
                services.AddInfrastructure(connectionString, useSqlServer);

                // Services layer (Business logic)
                services.AddServices();

                // GUI layer (ViewModels + UI Services)
                services.AddGUI();

                // MainWindow + LoginView (ViewModels già registrati in AddGUI)
                services.AddTransient<MainWindow>();
                services.AddTransient<LoginView>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        Services = _host.Services;

        // Crea/aggiorna il DB e popola con dati iniziali se vuoto.
        // Retry loop: se il DB non è raggiungibile, mostra dialog Riprova/Esci.
        while (true)
        {
            try
            {
                using var scope = _host.Services.CreateScope();
                var dbContext = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                // SQL Server: applica migrations versionati
                // SQLite: ricrea schema dal modello (migrations sono SQL Server-only)
                if (dbContext.Database.IsSqlServer())
                    await dbContext.Database.MigrateAsync();
                else
                    await dbContext.Database.EnsureCreatedAsync();

                await DatabaseSeeder.SeedAsync(dbContext);
                break;
            }
            catch (Exception ex)
            {
                var retry = DarkDialog.ShowConfirm(
                    "Errore connessione database",
                    "Impossibile connettersi al database.\n\n"
                    + ex.Message
                    + "\n\nRiprovare?");

                if (!retry)
                {
                    Shutdown();
                    return;
                }
            }
        }

        // Configura MainWindow
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow; // Esplicito: evita che un DarkDialog di startup resti come MainWindow
        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainViewModel;

        // Sottoscrivi all'evento di logout per mostrare di nuovo la LoginView
        mainViewModel.LoggedOut += () => ShowLoginView(mainViewModel);

        // Mostra LoginView all'avvio
        ShowLoginView(mainViewModel);

        mainWindow.Show();

        base.OnStartup(e);
    }

    /// <summary>
    /// Mostra la view di login e gestisce il callback.
    /// </summary>
    private async void ShowLoginView(MainViewModel mainViewModel)
    {
        var loginViewModel = _host.Services.GetRequiredService<LoginViewModel>();
        var loginView = _host.Services.GetRequiredService<LoginView>();
        loginView.DataContext = loginViewModel;

        // Pulisce sottoscrizioni precedenti per evitare chiamate duplicate
        loginViewModel.ClearSubscriptions();

        // Quando l'utente conferma il login, imposta l'utente e naviga
        loginViewModel.LoginConfirmed += user =>
        {
            mainViewModel.SetUserAndNavigate(user);
        };

        mainViewModel.CurrentViewModel = loginView;
        mainViewModel.PageTitle = "Login";

        // Carica gli utenti dal database
        await loginViewModel.LoadUsersAsync();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
