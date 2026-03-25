# GUI.Windows

> **Applicazione WPF desktop per la gestione dei dizionari STEM.**  
> **Ultimo aggiornamento:** 2026-03-25

---

## Panoramica

Il progetto **GUI.Windows** è l'interfaccia utente desktop per Stem.Dictionaries.Manager. Implementa:

- **WPF + MVVM** - Pattern Model-View-ViewModel con CommunityToolkit.Mvvm
- **Dependency Injection** - Microsoft.Extensions.Hosting per DI/configurazione
- **Navigation Service** - Navigazione tra view con history e parametri
- **Clean Architecture** - UI disaccoppiata da business logic e persistence
- **Stili Riutilizzabili** - Dark theme VS Code-style con stili globali
- **Input Validation** - Filtri hex/numerico con converter nullable
- **Ricerca Client-Side** - Filtro istantaneo in tutte le liste (case-insensitive)

L'applicazione si avvia con login integrato nella MainWindow, poi applica migrations e popola dati demo se DB vuoto.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Login Integrato** | ✅ | LoginView nella MainWindow, eventi LoginConfirmed/LoggedOut |
| **Dark Theme** | ✅ | VS Code-style con sidebar, header, DataGrid dark |
| **MVVM Pattern** | ✅ | 15 ViewModels con CommunityToolkit.Mvvm |
| **Views** | ✅ | 14 Views XAML complete (13 + LoginView) |
| **Converters** | ✅ | 5 converter (Bool, Inverse, Null, NullableInt, NullableDouble) |
| **Stili Globali** | ✅ | Sidebar, Toolbar, Watermark, DataGrid, Accent, HexAddress |
| **Navigation Service** | ✅ | History, parametri, eventi |
| **Dialog Service** | ✅ | Conferme, messaggi, errori |
| **Message Service** | ✅ | StatusBar e notifiche |
| **DI Container** | ✅ | Generic Host pattern |
| **Auto-Migration** | ✅ | EF Core migrations all'avvio |
| **Database Seeder** | ✅ | Dati demo per sviluppo |
| **BitInterpretations** | ✅ | Gestione bit per variabili Bitmapped (WordGroups, max 16 bit/word) |

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

1. L'app crea il database SQLite in `%AppData%\STEM\DictionariesManager\`
2. Applica automaticamente le migrations EF Core
3. Popola con dati demo (utenti, dizionari, variabili di esempio)
4. Mostra la LoginView integrata — selezionare un utente e premere ACCEDI
5. Sidebar e header diventano visibili, naviga alla lista dizionari

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
│   ├── MainViewModel.cs           # Shell, navigazione, CurrentUser, login/logout
│   ├── LoginViewModel.cs          # Login: carica utenti, conferma login
│   ├── DeviceListViewModel.cs     # Lista dispositivi (enum DeviceType)
│   ├── DeviceDetailViewModel.cs   # Dettaglio device: dizionari e schede
│   ├── DictionaryListViewModel.cs # Lista dizionari CRUD (SemanticDisplay)
│   ├── DictionaryEditViewModel.cs # Dettaglio/modifica dizionario (IsStandard)
│   ├── VariableListViewModel.cs   # Lista variabili di un dizionario
│   ├── VariableEditViewModel.cs   # Crea/modifica variabile (AddressHigh computed da Dictionary.IsStandard)
│   ├── CommandListViewModel.cs    # Lista comandi protocollo
│   ├── CommandEditViewModel.cs    # Crea/modifica comando (CodeHigh computed da IsResponse)
│   ├── BoardListViewModel.cs      # Lista schede
│   ├── BoardEditViewModel.cs      # Crea/modifica scheda (FirmwareType, DictionaryId?)
│   ├── UserListViewModel.cs       # Lista utenti con add inline
│   ├── SettingsViewModel.cs       # Impostazioni app (stub)
│   ├── WordBitGroup.cs            # Gruppo bit per word (max 16, Bitmapped)
│   └── BitInterpretationItem.cs   # Item singolo bit (WordIndex, BitIndex, Meaning)
├── Views/
│   ├── LoginView.xaml             # Login integrato nella MainWindow
│   ├── DeviceListView.xaml        # UI lista dispositivi
│   ├── DeviceDetailView.xaml      # UI dettaglio device
│   ├── DictionaryListView.xaml    # UI lista dizionari
│   ├── DictionaryEditView.xaml    # UI edit dizionario (IsStandard checkbox)
│   ├── VariableListView.xaml      # UI lista variabili
│   ├── VariableEditView.xaml      # UI edit variabile + DeviceStates
│   ├── CommandListView.xaml       # UI lista comandi
│   ├── CommandEditView.xaml       # UI edit comando
│   ├── BoardListView.xaml         # UI lista schede
│   ├── BoardEditView.xaml         # UI edit scheda (FirmwareType, DictionaryId?)
│   ├── UserListView.xaml          # UI lista utenti
│   └── SettingsView.xaml          # UI impostazioni
├── Converters/
│   ├── Converters.cs              # BoolToVisibility, InverseBool, NullToVisibility
│   └── NullableNumericConverter.cs # NullableInt, NullableDouble converters
├── App.xaml                       # Application resources + dark theme styles
├── App.xaml.cs                    # Startup, DI, ShowLoginView
├── MainWindow.xaml                # Shell: sidebar + header + content
├── MainWindow.xaml.cs             # Window code-behind + shutdown
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
| `DeviceList` | DeviceListViewModel | Lista dispositivi STEM |
| `DeviceDetail` | DeviceDetailViewModel | Dettaglio device: dizionari e schede |
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
    NavigationParameter? CurrentParameter { get; }
    bool CanGoBack { get; }

    void NavigateTo(ViewType viewType, NavigationParameter? parameter = null);
    bool GoBack();

    event EventHandler<ViewType>? CurrentViewChanged;
}
```

### IDialogService

```csharp
public enum DialogResult { Ok, Cancel, Yes, No }

public interface IDialogService
{
    Task<DialogResult> ShowConfirmAsync(string title, string message);
    Task<DialogResult> ShowOkCancelAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task ShowInfoAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
}
```

### IMessageService

```csharp
public enum MessageSeverity { Info, Success, Warning, Error }

public interface IMessageService
{
    string? CurrentMessage { get; }
    MessageSeverity CurrentSeverity { get; }

    void Show(string message, MessageSeverity severity = MessageSeverity.Info, int autoHideSeconds = 5);
    void Clear();

    event EventHandler? MessageChanged;
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

→ [GUI.Windows/ISSUES.md](./ISSUES.md) — 4 issue aperte, 4 risolte (0 critiche, 1 alta, 1 media, 2 basse)

---

## Links

- [Services/README.md](../Services/README.md) - Business logic layer
- [Infrastructure/README.md](../Infrastructure/README.md) - Data persistence
- [Tests/README.md](../Tests/README.md) - Test suite (include GUI tests)
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Architettura e decisioni
