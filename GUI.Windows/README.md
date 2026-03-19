# GUI.Windows

> **Applicazione WPF desktop per la gestione dei dizionari STEM.**  
> **Ultimo aggiornamento:** 2026-03-19

---

## Panoramica

Il progetto **GUI.Windows** è l'interfaccia utente desktop per Stem.Dictionaries.Manager. Implementa:

- **WPF + MVVM** - Pattern Model-View-ViewModel con CommunityToolkit.Mvvm
- **Dependency Injection** - Microsoft.Extensions.Hosting per DI/configurazione
- **Navigation Service** - Navigazione tra view con history e parametri
- **Clean Architecture** - UI disaccoppiata da business logic e persistence

L'applicazione si avvia con database SQLite locale, applica migrations automaticamente e popola dati demo se vuoto.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **MVVM Pattern** | ✅ | 11 ViewModels con CommunityToolkit.Mvvm |
| **Navigation Service** | ✅ | History, parametri, eventi |
| **Dialog Service** | ✅ | Conferme, messaggi, errori |
| **Message Service** | ✅ | StatusBar e notifiche |
| **DI Container** | ✅ | Generic Host pattern |
| **Auto-Migration** | ✅ | EF Core migrations all'avvio |
| **Database Seeder** | ✅ | Dati demo per sviluppo |

---

## Requisiti

- **.NET 10.0** o superiore
- **Windows 10/11** (WPF)

### Dipendenze

| Package | Versione | Uso |
|---------|----------|-----|
| CommunityToolkit.Mvvm | 8.4.0 | MVVM + Source Generators |
| Microsoft.Extensions.Hosting | 10.0.0-preview | DI container + hosting |

### Dipendenze Progetto

| Progetto | Uso |
|----------|-----|
| Infrastructure | DbContext, Repositories |
| Services | Business logic, DTOs |

---

## Quick Start

```bash
# Avviare l'applicazione (da solution root)
dotnet run --project GUI.Windows

# Oppure da Visual Studio: Set as Startup Project → F5
```

### Primo Avvio

1. L'app crea il database SQLite in `%AppData%\Stem.Dictionaries.Manager\`
2. Applica automaticamente le migrations EF Core
3. Popola con dati demo (utenti, dizionari, variabili di esempio)
4. Mostra la lista dizionari

---

## Struttura

```
GUI.Windows/
├── Abstractions/
│   ├── INavigationService.cs      # Interfaccia navigazione + ViewType enum
│   ├── IDialogService.cs          # Interfaccia dialoghi (conferme, errori)
│   └── IMessageService.cs         # Interfaccia messaggi (status bar)
├── Services/
│   ├── NavigationService.cs       # Implementazione con history stack
│   ├── DialogService.cs           # MessageBox wrapper
│   └── MessageService.cs          # Status notifications
├── ViewModels/
│   ├── MainViewModel.cs           # Shell principale, navigazione
│   ├── DictionaryListViewModel.cs # Lista dizionari CRUD
│   ├── DictionaryEditViewModel.cs # Dettaglio/modifica dizionario
│   ├── VariableListViewModel.cs   # Lista variabili di un dizionario
│   ├── VariableEditViewModel.cs   # Crea/modifica variabile
│   ├── CommandListViewModel.cs    # Lista comandi protocollo
│   ├── CommandEditViewModel.cs    # Crea/modifica comando
│   ├── BoardListViewModel.cs      # Lista schede
│   ├── BoardEditViewModel.cs      # Crea/modifica scheda
│   ├── UserListViewModel.cs       # Lista utenti con add inline
│   └── SettingsViewModel.cs       # Impostazioni app (stub)
├── Views/
│   ├── DictionaryListView.xaml    # UI lista dizionari
│   └── DictionaryEditView.xaml    # UI edit dizionario
├── Converters/
│   └── Converters.cs              # Value converters WPF
├── App.xaml                       # Application resources
├── App.xaml.cs                    # Startup, DI configuration
├── MainWindow.xaml                # Shell window
├── MainWindow.xaml.cs             # Window code-behind
└── DependencyInjection.cs         # AddGUI() extension method
```

---

## Architettura

### Pattern MVVM

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│    View     │────▶│  ViewModel   │────▶│   Service    │
│   (XAML)    │     │ (Observable) │     │  (Business)  │
└─────────────┘     └──────────────┘     └──────────────┘
      │                    │                    │
      │ DataBinding        │ Commands           │ Async
      ▼                    ▼                    ▼
   UI Updates         User Actions         Domain Logic
```

### Navigation Flow

```csharp
// Navigare alla lista dizionari
_navigationService.NavigateTo(ViewType.DictionaryList);

// Navigare all'edit con parametro
_navigationService.NavigateTo(ViewType.DictionaryEdit, 
    new NavigationParameter { EntityId = 42 });

// Tornare indietro
_navigationService.GoBack();
```

### ViewType Enum

| ViewType | ViewModel | Descrizione |
|----------|-----------|-------------|
| `DictionaryList` | DictionaryListViewModel | Lista dizionari |
| `DictionaryEdit` | DictionaryEditViewModel | Crea/modifica dizionario |
| `VariableList` | VariableListViewModel | Lista variabili |
| `VariableEdit` | VariableEditViewModel | Crea/modifica variabile |
| `CommandList` | CommandListViewModel | Lista comandi |
| `CommandEdit` | CommandEditViewModel | Crea/modifica comando |
| `BoardList` | BoardListViewModel | Lista schede |
| `BoardEdit` | BoardEditViewModel | Crea/modifica scheda |
| `UserList` | UserListViewModel | Lista utenti |
| `Settings` | SettingsViewModel | Impostazioni app (stub) |

---

## API / Componenti

### INavigationService

```csharp
public interface INavigationService
{
    ViewType CurrentView { get; }
    bool CanGoBack { get; }
    
    void NavigateTo(ViewType viewType, NavigationParameter? parameter = null);
    bool GoBack();
    
    event EventHandler<ViewType>? CurrentViewChanged;
}
```

### IDialogService

```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string message, string title);
    Task ShowErrorAsync(string message, string title);
    Task ShowInfoAsync(string message, string title);
}
```

### IMessageService

```csharp
public interface IMessageService
{
    void ShowMessage(string message);
    void ShowError(string message);
    void Clear();
}
```

### DI Registration

```csharp
// In App.xaml.cs
services.AddInfrastructure(connectionString);  // DbContext + Repos
services.AddServices();                         // Business logic
services.AddGUI();                              // ViewModels + UI Services
```

---

## Configurazione

### Database Location

Il database SQLite è creato in:
```
%AppData%\Stem.Dictionaries.Manager\dictionaries.db
```

### Logging

Il Generic Host configura automaticamente il logging. Per debug verbose:
```csharp
.ConfigureLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
})
```

---

## Issue Correlate

→ [GUI.Windows/ISSUES.md](./ISSUES.md) — 2 issue aperte, 1 risolta (0 critiche, 0 alte, 0 medie, 2 basse)

---

## Links

- [Services/README.md](../Services/README.md) - Business logic layer
- [Infrastructure/README.md](../Infrastructure/README.md) - Data persistence
- [Tests/README.md](../Tests/README.md) - Test suite (include GUI tests)
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Architettura e decisioni
