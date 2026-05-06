# GUI.Windows - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, UX issues, opportunità di refactoring e violazioni di best practice per il componente **GUI.Windows**.

> **Ultimo aggiornamento:** 2026-04-13

> ⚠️ **Open items migrated to GitHub Issues (2026-05-06).** This in-tree file is being retired as part of the upcoming `llm-settings v1.2.1` standards-adoption PR. Map: GUI-002 → [#15](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/issues/15), GUI-003 → [#16](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/issues/16).

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 3 |
| **Media** | 0 | 4 |
| **Bassa** | 2 | 1 |

**Totale aperte:** 2  
**Totale risolte:** 8

---

## Indice Issue Aperte

- [GUI-002 - App.Services è static e impedisce testabilità](#gui-002--appservices-è-static-e-impedisce-testabilità)
- [GUI-003 - DialogService usa MessageBox sincrono wrappato in Task](#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task)

## Indice Issue Risolte

- [GUI-010 - Gestione errore connessione DB all'avvio](#gui-010--gestione-errore-connessione-db-allavvio)
- [GUI-009 - Rimuovere DeviceVariables, aggiornare DictionaryEdit (T-006)](#gui-009--rimuovere-devicevariables-aggiornare-dictionaryedit-t-006)
- [GUI-006 - LoginViewModel registrato due volte nel DI container](#gui-006--loginviewmodel-registrato-due-volte-nel-di-container)
- [GUI-005 - MainViewModel.NavigateToView è async void senza error handling](#gui-005--mainviewmodelnavigatetoview-è-async-void-senza-error-handling)
- [GUI-008 - Refactoring GUI per Domain v2](#gui-008--refactoring-gui-per-domain-v2)
- [GUI-007 - DictionaryListItem non mostra DeviceType (semantica Dedicato)](#gui-007--dictionarylistitem-non-mostra-devicetype-semantica-dedicato)
- [GUI-001 - Mancano ViewModels per tutte le ViewType dichiarate](#gui-001--mancano-viewmodels-per-tutte-le-viewtype-dichiarate)
- [GUI-004 - Refactoring grafico completo e migrazione login](#gui-004--refactoring-grafico-completo-e-migrazione-login)

---

## Priorità Media

*(Nessuna issue media priorità aperta)*

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

### GUI-003 - DialogService wrappa sync in Task

**Categoria:** UX/Design  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-19  
**Aggiornamento:** 2026-04-10 — `DarkDialog` custom sostituisce `MessageBox.Show()`, ma il pattern sync-wrappato-in-Task rimane.

#### Descrizione

`DialogService` espone metodi async ma internamente usa `DarkDialog.ShowConfirm()` (modale WPF `ShowDialog()`) che è sincrono. Il risultato è wrappato in `Task.FromResult`.

#### File Coinvolti

- `GUI.Windows/Services/DialogService.cs`

#### Codice Attuale

```csharp
public sealed class DialogService : IDialogService
{
    public Task<DialogResult> ShowConfirmAsync(string title, string message)
    {
        var result = DarkDialog.ShowConfirm(title, message); // sync ShowDialog()
        return Task.FromResult(result ? DialogResult.Yes : DialogResult.No);
    }
}
```

#### Problema Specifico

- API async ma implementazione sync (misleading)
- `ShowDialog()` blocca il message pump del thread UI
- Per ora accettabile (desktop app, UX soddisfacente con DarkDialog)

#### Soluzione Proposta

Accettare il pattern attuale come **low priority** — il DarkDialog custom è già themed correttamente e funziona bene. Se servisse vero async in futuro, wrappare `ShowDialog()` con `TaskCompletionSource`.

#### Benefici Attesi

- API coerente (sync o async, non finto async)
- Sblocco thread UI durante dialog

---

---

## Issue Risolte

### GUI-010 — Gestione errore connessione DB all'avvio

**Categoria:** Robustezza/UX  
**Priorità:** Bassa  
**Impatto:** Medio — crash non gestito se il DB non è raggiungibile  
**Status:** ✅Risolto  
**Data Apertura:** 2026-04-13  
**Data Risoluzione:** 2026-04-13  
**Branch:** fix/gui-010-api-004

#### Soluzione Implementata

1. **`App.xaml.cs`**: Blocco inizializzazione DB wrappato in `while(true)` + `try/catch(Exception)`
   - Errore → `DarkDialog.ShowConfirm` con messaggio + `ex.Message` + opzioni Riprova/Esci
   - Riprova → nuovo scope DI + nuovo tentativo (DbContext fresco)
   - Esci → `Shutdown()` + `return` (uscita pulita)
2. **`DarkDialog.xaml.cs`**: Fix `Owner` durante startup — WPF assegna `MainWindow` alla prima `Window` istanziata:
   - Se `MainWindow` è `null` o è il dialog stesso → `CenterScreen` (fallback)
   - Se `MainWindow` è disponibile → `Owner = MainWindow` (centrato sulla finestra)
3. **`App.xaml.cs`**: Assegnamento esplicito `MainWindow = mainWindow` dopo creazione, per evitare che un `DarkDialog` di startup resti come `MainWindow` dell'applicazione

#### File Modificati

- `GUI.Windows/App.xaml.cs` (retry loop + MainWindow esplicito)
- `GUI.Windows/Views/DarkDialog.xaml.cs` (Owner null-safe)

#### Benefici Ottenuti

- Messaggio chiaro all'utente invece di crash con stacktrace ✅
- Possibilità di riprovare senza riavviare l'app ✅
- Gestione graceful di interruzioni temporanee di rete ✅
- DarkDialog sicuro durante startup (nessun crash Owner=self) ✅
- MainWindow assegnata correttamente dopo retry riuscito ✅

---

## Issue Risolte

### GUI-009 - Rimuovere DeviceVariables, aggiornare DictionaryEdit (T-006)

**Categoria:** Refactoring  
**Priorità:** Alta  
**Impatto:** Alto — cambiamento di dominio fondamentale  
**Status:** ✅Risolto  
**Data Apertura:** 2026-03-30  
**Data Risoluzione:** 2026-04-07  
**Branch:** fix/t-006  
**Parent Issue:** [T-006](../ISSUES_TRACKER.md#t-006--standardvariableoverride-per-dizionario-domain-v7)

#### Soluzione Implementata

1. **Eliminati** `DeviceVariablesViewModel.cs`, `VariableDeviceItem.cs`, `DeviceVariablesView.xaml(.cs)`
2. **Rimosso** `DeviceVariables` da `ViewType` enum
3. **Rimossi** 3 case `DeviceVariables` da `MainViewModel` (CreateViewModel, InitializeViewModelAsync, UpdateTitle)
4. **Rimosso** DataTemplate `DeviceVariablesView` da `MainWindow.xaml`
5. **Modificato** `DeviceDetailViewModel`: Standard → DictionaryEdit (non più DeviceVariables)
6. **Rimossa** registrazione `DeviceVariablesViewModel` da DI
7. **Rinominato** `VariableEditViewModel`: DeviceContext → DictionaryContext, `_deviceContextId` → `_dictionaryContextId`, API `*ForDevice*` → `*ForDictionary*`, `SaveDeviceStateAsync` → `SaveOverrideAsync`
8. **Aggiornato** `VariableEditView.xaml`: binding `IsDeviceContext` → `IsDictionaryContext`

#### Benefici Ottenuti

- DeviceVariablesView eliminata (vista non più necessaria) ✅
- Standard dizionario → DictionaryEdit (come tutti gli altri dizionari) ✅
- VariableEdit DictionaryContext per-dizionario (non per-device) ✅
- Build verde ✅

---

### GUI-006 - LoginViewModel registrato due volte nel DI container

**Categoria:** Code Smell  
**Priorità:** Media  
**Impatto:** Basso  
**Status:** ✅Risolto  
**Data Apertura:** 2026-03-24  
**Data Risoluzione:** 2026-03-25  
**Branch:** fix/gui-006

#### Soluzione Implementata

Rimossa registrazione duplicata da `App.xaml.cs` riga 47. La registrazione in `DependencyInjection.AddGUI()` è quella corretta (centralizzata).

```csharp
// App.xaml.cs - prima
services.AddTransient<LoginViewModel>();   // ← duplicato rimosso

// App.xaml.cs - dopo
// MainWindow + LoginView (ViewModels già registrati in AddGUI)
services.AddTransient<MainWindow>();
services.AddTransient<LoginView>();
```

#### Benefici Ottenuti

- Registrazione DI centralizzata e senza duplicati ✅
- Codice più pulito ✅

---

### GUI-005 - MainViewModel.NavigateToView è async void senza error handling

**Categoria:** Bug (Anti-Pattern)  
**Priorità:** Alta  
**Impatto:** Alto  
**Status:** Risolto  
**Data Apertura:** 2026-03-24  
**Data Risoluzione:** 2026-03-25  
**Branch:** fix/gui-005

#### Soluzione Implementata

1. Aggiunto `try/catch(Exception)` in `NavigateToView` (async void)
2. Nel catch: `CurrentViewModel = null`, `UpdateTitle(viewType)`, messaggio errore permanente in status bar via `_messageService.Show(..., MessageSeverity.Error, autoHideSeconds: 0)`
3. Protegge da: errori DI resolution, eccezioni non gestite in `InitializeViewModelAsync`, futuri ViewModel senza try/catch interno

#### Test Aggiunti

- `NavigateToView_WhenCreateViewModelThrows_DoesNotCrash` — DI factory lancia → nessun crash, stato coerente
- `NavigateToView_WhenCreateViewModelThrows_ShowsErrorInStatusBar` — verifica messaggio `MessageSeverity.Error` nella status bar

#### Benefici Ottenuti

- Nessun crash dell'app su errori di navigazione ✅
- UX resiliente: messaggio errore in status bar invece di crash ✅
- Difesa in profondità: try/catch esterno + try/catch interni nei ViewModel ✅

---

### GUI-008 - Refactoring GUI per Domain v2

**Categoria:** Refactoring  
**Priorità:** Alta  
**Impatto:** Alto  
**Status:** Risolto  
**Data Apertura:** 2026-03-25  
**Data Risoluzione:** 2026-03-25  
**Branch:** domain/ridefinizione-dominio-v2  
**Master Issue:** T-002

#### Soluzione Implementata

1. `DictionaryEditViewModel`: CheckBox `IsStandard`, rimosso DeviceType/BoardType
2. `DictionaryListViewModel`: colonna `SemanticDisplay` derivata (Standard/Specifico)
3. `BoardEditViewModel`: FirmwareType diretto, DictionaryId nullable
4. `DeviceDetailViewModel`: dizionari derivati da Board→Dictionary + Standard

#### Benefici Ottenuti

- GUI allineata al Domain v2 ✅
- Risolve anche GUI-007 ✅

---

### GUI-007 - DictionaryListItem non mostra DeviceType (semantica Dedicato)

**Categoria:** UX  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-24  
**Data Risoluzione:** 2026-03-25  
**Branch:** domain/ridefinizione-dominio-v2

#### Soluzione Implementata

Il bug non esiste più: la semantica 3-tuple è stata sostituita con `IsStandard` flag. `DictionaryListViewModel` mostra `SemanticDisplay` ("Standard"/"Specifico") derivata. La colonna "Usato da" potrà essere aggiunta come feature separata.

---

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
