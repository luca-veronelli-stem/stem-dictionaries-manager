using GUI.Windows.ViewModels;
using GUI.Windows.Views;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
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
                // Database path in AppData
                var connectionString = $"Data Source={GetDatabasePath()}";

                // Infrastructure layer (DbContext + Repositories)
                services.AddInfrastructure(connectionString);

                // Services layer (Business logic)
                services.AddServices();

                // GUI layer (ViewModels + UI Services)
                services.AddGUI();

                // MainWindow + LoginView
                services.AddTransient<MainWindow>();
                services.AddTransient<LoginView>();
                services.AddTransient<LoginViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();
        Services = _host.Services;

        // Applica tutte le migrations pendenti (crea DB se non esiste)
        // e popola con dati di esempio se il DB è vuoto
        using (var scope = _host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
            await DatabaseSeeder.SeedAsync(dbContext);
        }

        // Configura MainWindow
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
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

    /// <summary>
    /// Restituisce il path del database SQLite in AppData.
    /// Crea la cartella se non esiste.
    /// </summary>
    private static string GetDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "STEM", "DictionariesManager");

        Directory.CreateDirectory(folder);

        return Path.Combine(folder, "data.db");
    }
}

