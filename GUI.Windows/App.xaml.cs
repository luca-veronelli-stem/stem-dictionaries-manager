using System.IO;
using System.Windows;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using GUI.Windows.Views;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using Services.Interfaces;

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

                // MainWindow
                services.AddTransient<MainWindow>();
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

        // Mostra finestra selezione utente
        var userService = _host.Services.GetRequiredService<IUserService>();
        var users = await userService.GetAllAsync();

        if (users.Count == 0)
        {
            MessageBox.Show("Nessun utente nel database.", "Errore",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var selectionWindow = new UserSelectionWindow(users);
        if (selectionWindow.ShowDialog() != true || selectionWindow.SelectedUser is null)
        {
            Shutdown();
            return;
        }

        // Imposta utente corrente
        var currentUserService = _host.Services.GetRequiredService<ICurrentUserService>();
        currentUserService.SetCurrentUser(selectionWindow.SelectedUser);

        // Configura e mostra MainWindow
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();

        base.OnStartup(e);
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

