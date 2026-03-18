# GUI.Windows - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, UX issues, opportunità di refactoring e violazioni di best practice per il componente **GUI.Windows**.

> **Ultimo aggiornamento:** 2026-03-19

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 1 | 0 |
| **Bassa** | 2 | 0 |

**Totale aperte:** 3  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [GUI-001 - Mancano ViewModels per tutte le ViewType dichiarate](#gui-001--mancano-viewmodels-per-tutte-le-viewtype-dichiarate)
- [GUI-002 - App.Services è static e impedisce testabilità](#gui-002--appservices-è-static-e-impedisce-testabilità)
- [GUI-003 - DialogService usa MessageBox sincrono wrappato in Task](#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task)

## Indice Issue Risolte

*(Nessuna issue risolta)*

---

## Copertura Attuale

| Componente | Test Unit | Test Integration | Copertura |
|------------|:---------:|:----------------:|-----------|
| ViewModels (3) | ✅ 38 | - | ~80% |
| Services (3) | ✅ 12 | - | ~70% |
| DependencyInjection | ✅ 13 | - | 100% |
| Views (2) | - | - | N/A (XAML) |
| Converters | - | - | N/A |

---

## Priorità Media

### GUI-001 - Mancano ViewModels per tutte le ViewType dichiarate

**Categoria:** Struttura/Completezza  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-19  

#### Descrizione

L'enum `ViewType` dichiara 10 tipi di view, ma solo 3 hanno ViewModel implementati. Navigare verso le altre view causerà eccezioni o comportamenti undefined.

#### File Coinvolti

- `GUI.Windows/Abstractions/INavigationService.cs` (ViewType enum)
- `GUI.Windows/Services/NavigationService.cs` (switch/mapping)
- `GUI.Windows/DependencyInjection.cs` (registrazioni mancanti)

#### Codice Problematico

```csharp
// ViewType enum - 10 tipi dichiarati
public enum ViewType
{
    DictionaryList,    // ✅ Implementato
    DictionaryEdit,    // ✅ Implementato
    VariableList,      // ❌ Manca ViewModel
    VariableEdit,      // ❌ Manca ViewModel
    CommandList,       // ❌ Manca ViewModel
    CommandEdit,       // ❌ Manca ViewModel
    BoardList,         // ❌ Manca ViewModel
    BoardEdit,         // ❌ Manca ViewModel
    UserList,          // ❌ Manca ViewModel
    Settings           // ❌ Manca ViewModel
}
```

#### Problema Specifico

- NavigationService potrebbe crashare su ViewType non gestiti
- UI potrebbe mostrare pulsanti che portano a view inesistenti
- Inconsistenza tra API dichiarata e implementazione

#### Soluzione Proposta

**Opzione A: Implementare progressivamente**

Creare ViewModel stub per ogni ViewType mancante:

```csharp
public partial class VariableListViewModel : ObservableObject
{
    // TODO: Implementare nella prossima iterazione
}
```

**Opzione B: Ridurre l'enum**

Rimuovere temporaneamente i ViewType non implementati e aggiungerli quando i ViewModel sono pronti:

```csharp
public enum ViewType
{
    DictionaryList,
    DictionaryEdit,
    // Altri aggiunti con le implementazioni
}
```

#### Benefici Attesi

- API consistente con implementazione
- No runtime errors su navigazione
- Roadmap chiara per feature mancanti

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

*(Nessuna issue risolta)*

---

## Wontfix

*(Nessuna issue in wontfix)*
