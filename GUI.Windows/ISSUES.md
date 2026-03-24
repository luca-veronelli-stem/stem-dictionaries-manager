# GUI.Windows - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, UX issues, opportunità di refactoring e violazioni di best practice per il componente **GUI.Windows**.

> **Ultimo aggiornamento:** 2026-03-24

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 1 | 0 |
| **Media** | 2 | 2 |
| **Bassa** | 2 | 0 |

**Totale aperte:** 5  
**Totale risolte:** 2

---

## Indice Issue Aperte

- [GUI-005 - MainViewModel.NavigateToView è async void senza error handling](#gui-005--mainviewmodelnavigatetoview-è-async-void-senza-error-handling)
- [GUI-006 - LoginViewModel registrato due volte nel DI container](#gui-006--loginviewmodel-registrato-due-volte-nel-di-container)
- [GUI-007 - DictionaryListItem non mostra DeviceType (semantica Dedicato)](#gui-007--dictionarylistitem-non-mostra-devicetype-semantica-dedicato)
- [GUI-002 - App.Services è static e impedisce testabilità](#gui-002--appservices-è-static-e-impedisce-testabilità)
- [GUI-003 - DialogService usa MessageBox sincrono wrappato in Task](#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task)

## Indice Issue Risolte

- [GUI-001 - Mancano ViewModels per tutte le ViewType dichiarate](#gui-001--mancano-viewmodels-per-tutte-le-viewtype-dichiarate)
- [GUI-004 - Refactoring grafico completo e migrazione login](#gui-004--refactoring-grafico-completo-e-migrazione-login)

---

## Priorità Alta

### GUI-005 - MainViewModel.NavigateToView è async void senza error handling

**Categoria:** Bug (Anti-Pattern)  
**Priorità:** Alta  
**Impatto:** Alto  
**Status:** Aperto  
**Data Apertura:** 2026-03-24  

#### Descrizione

`MainViewModel.NavigateToView` è `async void` e non ha try/catch. Se `InitializeViewModelAsync` lancia un'eccezione (es. DB non raggiungibile, service failure), l'eccezione non viene gestita e **crasha l'applicazione** con `UnhandledTaskException`.

#### File Coinvolti

- `GUI.Windows/ViewModels/MainViewModel.cs` (righe 89-101)

#### Codice Problematico

```csharp
private async void NavigateToView(ViewType viewType, NavigationParameter? parameter)
{
    var viewModel = CreateViewModel(viewType);

    if (viewModel is not null)
    {
        // Se questo lancia, l'app crasha (async void non cattura)
        await InitializeViewModelAsync(viewModel, parameter);
    }

    CurrentViewModel = viewModel;
    UpdateTitle(viewType);
}
```

#### Problema Specifico

- `async void` non consente al chiamante di osservare l'eccezione
- È invocato da `OnCurrentViewChanged` (event handler → `async void` è accettabile) ma il `try/catch` è comunque mancante
- Se un qualsiasi ViewModel.LoadAsync/InitializeAsync fallisce, l'app crasha
- Il pattern è particolarmente pericoloso perché **ogni navigazione** passa da qui
- `DeviceDetailViewModel.LoadAsync`, `DictionaryListViewModel.LoadAsync` ecc. hanno già `try/catch` interni, ma non tutti i path sono protetti

#### Soluzione Proposta

```csharp
private async void NavigateToView(ViewType viewType, NavigationParameter? parameter)
{
    try
    {
        var viewModel = CreateViewModel(viewType);

        if (viewModel is not null)
        {
            await InitializeViewModelAsync(viewModel, parameter);
        }

        CurrentViewModel = viewModel;
        UpdateTitle(viewType);
    }
    catch (Exception ex)
    {
        // Fallback: mostra errore senza crashare l'app
        CurrentViewModel = null;
        UpdateTitle(viewType);
        // Opzionale: mostra messaggio nella status bar
    }
}
```

#### Benefici Attesi

- Nessun crash dell'app su errori di navigazione
- UX resiliente: l'utente vede un messaggio di errore invece di un crash
- Coerenza: error handling a tutti i livelli della pipeline navigazione

---

## Priorità Media

### GUI-006 - LoginViewModel registrato due volte nel DI container

**Categoria:** Code Smell  
**Priorità:** Media  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-24  

#### Descrizione

`LoginViewModel` è registrato come `Transient` sia in `DependencyInjection.AddGUI()` (riga 26) che in `App.xaml.cs` (riga 47). La doppia registrazione non causa errori (l'ultima vince) ma è confusa e indica un residuo di refactoring.

#### File Coinvolti

- `GUI.Windows/DependencyInjection.cs` (riga 26)
- `GUI.Windows/App.xaml.cs` (riga 47)

#### Codice Problematico

```csharp
// DependencyInjection.cs - riga 26
services.AddTransient<LoginViewModel>();   // ← prima registrazione

// App.xaml.cs - riga 47
services.AddTransient<LoginViewModel>();   // ← duplicato
```

#### Soluzione Proposta

Rimuovere la registrazione duplicata da `App.xaml.cs` riga 47. La registrazione in `DependencyInjection.AddGUI()` è quella corretta (centralizzata).

```csharp
// App.xaml.cs - dopo
services.AddGUI();

// MainWindow + LoginView (Views, NON ViewModels)
services.AddTransient<MainWindow>();
services.AddTransient<LoginView>();
// LoginViewModel già registrato in AddGUI()
```

#### Benefici Attesi

- Registrazione DI centralizzata e senza duplicati
- Meno confusione nella manutenzione

---

### GUI-007 - DictionaryListItem non mostra DeviceType (semantica Dedicato)

**Categoria:** UX  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-24  

#### Descrizione

Dopo l'introduzione delle 3 semantiche di dizionario (SESSION_022), `DictionaryListItem` mostra solo `BoardTypeName` ma non il `DeviceType`. L'utente nella lista dizionari non distingue tra un dizionario **Dedicato** (`OptimusXp, Madre`) e una **Periferica condivisa** (`null, Madre`).

#### File Coinvolti

- `GUI.Windows/ViewModels/DictionaryListViewModel.cs` (righe 162-174)

#### Codice Attuale

```csharp
public class DictionaryListItem
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? BoardTypeName { get; init; }
    public int VariableCount { get; init; }

    // Manca: DeviceType? DeviceTypeName
    public string BoardTypeDisplay => BoardTypeName ?? "Standard";
}
```

#### Problema Specifico

- Due dizionari "Madre Optimus" e "Pulsantiere 4x4" hanno entrambi un BoardTypeName, ma uno è Dedicato e l'altro Condiviso
- L'utente non può distinguerli nella lista
- `BoardTypeDisplay` mostra "Standard" per `null`, ma non indica se il dizionario è condiviso o dedicato

#### Soluzione Proposta

Aggiungere `DeviceTypeName` e una proprietà `SemanticDisplay`:

```csharp
public class DictionaryListItem
{
    // ... existing
    public string? DeviceTypeName { get; init; }

    public string SemanticDisplay => (DeviceTypeName, BoardTypeName) switch
    {
        (null, null) => "Standard",
        (null, _) => $"Condiviso ({BoardTypeName})",
        (_, _) => $"{DeviceTypeName} — {BoardTypeName}"
    };
}
```

#### Benefici Attesi

- L'utente distingue le 3 semantiche nella lista
- UX coerente con la nuova architettura dizionari

---

## Priorità Bassa

### GUI-002 - App.Services è static e impedisce testabilità

**Categoria:** Design/Testabilità  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-19  

#### Descrizione

`App.Services` è una proprietà statica che espone l'`IServiceProvider`. Questo pattern rende difficile il testing e viola il principio di DI pura.

#### File Coinvolti

- `GUI.Windows/App.xaml.cs` (righe 23-24)

#### Codice Problematico

```csharp
public partial class App : Application
{
    /// <summary>
    /// Service provider per accesso ai servizi registrati.
    /// Usato dalle Views per creare dialog con DI.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;  // <-- Static
    
    // ...
    
    protected override async void OnStartup(StartupEventArgs e)
    {
        // ...
        Services = _host.Services;  // <-- Assegnato globalmente
    }
}
```

#### Problema Specifico

- Service Locator pattern (anti-pattern)
- Views accedono direttamente a `App.Services` invece di ricevere DI
- Difficile mockare nei test
- Dipendenza nascosta non esplicita

#### Soluzione Proposta

**Opzione A: Passare IServiceProvider alle Views**

```csharp
// MainWindow riceve il provider
public MainWindow(MainViewModel viewModel, IServiceProvider services)
{
    DataContext = viewModel;
    _services = services;
}

// Views usano il provider iniettato, non quello statico
```

**Opzione B: Usare ViewModelLocator**

```csharp
public class ViewModelLocator
{
    private readonly IServiceProvider _services;
    
    public ViewModelLocator(IServiceProvider services) => _services = services;
    
    public MainViewModel MainViewModel => _services.GetRequiredService<MainViewModel>();
    // ...
}
```

#### Benefici Attesi

- DI pura senza Service Locator
- Views più testabili
- Dipendenze esplicite

---

### GUI-003 - DialogService usa MessageBox sincrono wrappato in Task

**Categoria:** UX/Design  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-19  

#### Descrizione

`DialogService` espone metodi async ma internamente usa `MessageBox.Show()` che è sincrono. Questo può bloccare il thread UI.

#### File Coinvolti

- `GUI.Windows/Services/DialogService.cs`

#### Codice Problematico (probabile)

```csharp
public class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string message, string title)
    {
        // MessageBox.Show è sincrono!
        var result = MessageBox.Show(message, title, 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }
}
```

#### Problema Specifico

- API async ma implementazione sync (misleading)
- MessageBox blocca il message pump del thread UI
- Non permette animazioni o progress mentre il dialog è aperto
- Futuro: impossibile sostituire con dialog custom async

#### Soluzione Proposta

**Opzione A: Rinominare metodi sync**

Se sync è accettabile, rendere l'API onesta:

```csharp
public bool Confirm(string message, string title)
{
    return MessageBox.Show(...) == MessageBoxResult.Yes;
}
```

**Opzione B: Dialog WPF custom**

Creare dialog Window custom che supporta vero async:

```csharp
public async Task<bool> ConfirmAsync(string message, string title)
{
    var dialog = new ConfirmDialog(message, title);
    return await dialog.ShowDialogAsync();
}
```

#### Benefici Attesi

- API coerente (sync o async, non finto async)
- UX migliore con dialog custom
- Possibilità di theming/styling

---

## Issue Risolte

### GUI-001 - Mancano ViewModels per tutte le ViewType dichiarate

**Categoria:** Struttura/Completezza  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-19  
**Data Risoluzione:** 2026-03-19
**Branch:** gui/view-models-mancanti

#### Descrizione

L'enum `ViewType` dichiarava 10 tipi di view, ma solo 3 avevano ViewModel implementati.

#### Soluzione Implementata

Creati 8 nuovi ViewModels + aggiornati test e DI:

**ViewModels creati:**
- `VariableListViewModel.cs` - Lista variabili di un dizionario
- `VariableEditViewModel.cs` - Crea/modifica variabile
- `CommandListViewModel.cs` - Lista comandi protocollo
- `CommandEditViewModel.cs` - Crea/modifica comando
- `BoardListViewModel.cs` - Lista schede
- `BoardEditViewModel.cs` - Crea/modifica scheda
- `UserListViewModel.cs` - Lista utenti con add inline
- `SettingsViewModel.cs` - Impostazioni (stub)

**Test creati (105 nuovi test):**
- 14 test per VariableListViewModel
- 17 test per VariableEditViewModel
- 14 test per CommandListViewModel
- 16 test per CommandEditViewModel
- 13 test per BoardListViewModel
- 14 test per BoardEditViewModel
- 14 test per UserListViewModel
- 3 test per SettingsViewModel

**Mock services aggiunti:**
- `MockVariableService`
- `MockCommandService`
- `MockUserService`

**DI aggiornato:**
- Registrati tutti gli 11 ViewModels in `DependencyInjection.cs`

#### Benefici Ottenuti

- API ViewType 100% consistente con implementazione ✅
- No runtime errors su navigazione ✅
- Copertura test GUI da 63 a 172 test ✅

---

### GUI-004 - Refactoring grafico completo e migrazione login

**Categoria:** UX/Struttura  
**Priorità:** Media  
**Impatto:** Alto  
**Status:** Risolto  
**Data Apertura:** 2026-03-20  
**Data Risoluzione:** 2026-03-20  
**Branch:** gui/refactoring-completo

#### Descrizione

Refactoring grafico completo della GUI con dark theme VS Code-style e migrazione login da finestra modale separata a view integrata nella MainWindow (pattern Production.Tracker).

#### Modifiche Implementate

**Dark Theme (App.xaml):**
- Stili globali: SidebarButton, ToolbarButton, WatermarkTextBox, HexAddressTextBox, SearchTextBox
- DataGrid dark: DarkDataGridStyle, DarkDataGridColumnHeaderStyle, DarkDataGridRowStyle, DarkDataGridCellStyle
- AccentButton per azioni primarie (Salva, Accedi)
- Brush globali per colori consistenti

**Layout MainWindow:**
- Sidebar verticale con navigazione + logo + utente corrente
- Header con titolo pagina e pulsante Indietro
- Sidebar/Header visibili solo se `IsLoggedIn`
- DataGrid con `MinWidth` su tutte le colonne e `HorizontalScrollBarVisibility="Auto"`

**Migrazione Login:**
- Creato `LoginView` (UserControl integrato nella MainWindow)
- Creato `LoginViewModel` con `LoadUsersAsync`, `ConfirmLoginCommand`, evento `LoginConfirmed`
- `MainViewModel`: aggiunta property `CurrentUser` con `IsLoggedIn` computed
- `App.xaml.cs`: pattern eventi `LoggedOut`/`LoginConfirmed` (come PT)
- Rimosso `UserSelectionWindow` (finestra modale)
- Rimosso `CurrentUserService` (semplificato, `CurrentUser` direttamente in MainViewModel)

**Bug Fix:**
- ComboBox dropdown leggibile (stile light default)
- `CommandEditViewModel`: `CodeHighHex`/`CodeLowHex` string → no `FormatException` su input hex
- `BoardRepository.GetAllAsync()`: override con `Include(b => b.BoardType)` → no `BoardType not loaded`
- `ProtocolAddressDisplay` rimosso da `BoardEditView` (proprietà inesistente)
- Login button: `[NotifyCanExecuteChangedFor]` su `SelectedUser` e `IsLoading`
- App shutdown: `Application.Current.Shutdown()` in `MainWindow.Closing`

**File Eliminati:**
- `Views/UserSelectionWindow.xaml` + `.cs`
- `Services/CurrentUserService.cs`
- `Abstractions/ICurrentUserService.cs`

**Test (11 nuovi):**
- 8 test per `LoginViewModel`
- 3 test aggiuntivi per `MainViewModel` (IsLoggedIn, SetUserAndNavigate)
- Aggiornati test esistenti per riflettere nuovo flusso

#### Benefici Ottenuti

- UI dark theme consistente ✅
- Login integrato nella MainWindow (no flash finestra) ✅
- YAGNI: rimosso `CurrentUserService` non necessario ✅
- Bug binding hex risolti ✅
- 1112/1112 test verdi ✅

---

## Wontfix

*(Nessuna issue in wontfix)*
