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
- **Stili Riutilizzabili** - SearchTextBox, HexAddressTextBox, ToolbarButton
- **Input Validation** - Filtri hex/numerico con converter nullable
- **Ricerca Client-Side** - Filtro istantaneo in tutte le liste (case-insensitive)

L'applicazione si avvia con selezione utente, poi applica migrations e popola dati demo se DB vuoto.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Selezione Utente** | ✅ | Dialog modale all'avvio, ciclo login/logout |
| **MVVM Pattern** | ✅ | 11 ViewModels con CommunityToolkit.Mvvm |
| **Views** | ✅ | 11 Views XAML complete (10 + UserSelectionWindow) |
| **Converters** | ✅ | 5 converter (Bool, Inverse, Null, NullableInt, NullableDouble) |
| **Stili Globali** | ✅ | SearchTextBox, HexAddressTextBox, ToolbarButton |
| **Navigation Service** | ✅ | History, parametri, eventi |
| **Dialog Service** | ✅ | Conferme, messaggi, errori |
| **Message Service** | ✅ | StatusBar e notifiche |
| **Current User Service** | ✅ | Singleton, utente corrente per audit |
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
│   ├── IMessageService.cs         # Interfaccia messaggi (status bar)
│   └── ICurrentUserService.cs     # Interfaccia utente corrente (singleton)
├── Services/
│   ├── NavigationService.cs       # Implementazione con history stack
│   ├── DialogService.cs           # MessageBox wrapper
│   ├── MessageService.cs          # Status notifications
│   └── CurrentUserService.cs      # Utente corrente per audit
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
│   ├── UserSelectionWindow.xaml    # Dialog selezione utente all'avvio
│   ├── DictionaryListView.xaml    # UI lista dizionari
│   ├── DictionaryEditView.xaml    # UI edit dizionario
│   ├── VariableListView.xaml      # UI lista variabili
│   ├── VariableEditView.xaml      # UI edit variabile
│   ├── CommandListView.xaml       # UI lista comandi
│   ├── CommandEditView.xaml       # UI edit comando
│   ├── BoardListView.xaml         # UI lista schede
│   ├── BoardEditView.xaml         # UI edit scheda
│   ├── UserListView.xaml          # UI lista utenti
│   └── SettingsView.xaml          # UI impostazioni
├── Converters/
│   ├── Converters.cs              # BoolToVisibility, InverseBool, NullToVisibility
│   └── NullableNumericConverter.cs # NullableInt, NullableDouble converters
├── App.xaml                       # Application resources + stili globali
├── App.xaml.cs                    # Startup, DI, ciclo login/logout
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
