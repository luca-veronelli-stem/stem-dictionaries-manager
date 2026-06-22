#if WINDOWS
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Services;
using Tests.Shared;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Regression tests for the App.Services service-locator removal (#15).
/// DictionaryEditView no longer initializes its ViewModel from the navigation
/// service in code-behind, so MainViewModel must drive that initialization with
/// the navigation parameter when it creates the ViewModel.
/// </summary>
public class DictionaryEditNavigationTests
{
    private readonly MockNavigationService _navigationService = new();
    private readonly MockDialogService _dialogService = new();
    private readonly MockMessageService _messageService = new();
    private readonly MockDictionaryService _dictionaryService = new();
    private readonly MainViewModel _viewModel;

    public DictionaryEditNavigationTests()
    {
        ServiceCollection services = new();
        services.AddSingleton(_dictionaryService);
        services.AddSingleton<MockVariableService>();
        services.AddSingleton<MockBoardService>();
        services.AddSingleton<INavigationService>(_navigationService);
        services.AddSingleton<IDialogService>(_dialogService);
        services.AddSingleton<IMessageService>(_messageService);
        services.AddTransient(sp => new DictionaryEditViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<MockVariableService>(),
            sp.GetRequiredService<MockBoardService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>(),
            NullLogger<DictionaryEditViewModel>.Instance));
        IServiceProvider provider = services.BuildServiceProvider();

        _viewModel = new MainViewModel(
            _navigationService,
            _dialogService,
            _messageService,
            provider,
            new CurrentUserProvider(),
            NullLogger<MainViewModel>.Instance);
    }

    [Fact]
    public void NavigateToDictionaryEdit_WithEntityId_InitializesViewModelFromParameter()
    {
        global::Core.Models.Dictionary existing = new("Optimus-XP", "desc", isStandard: false);
        _dictionaryService.SeedData(existing); // seeded with Id 1

        _navigationService.NavigateTo(
            ViewType.DictionaryEdit,
            new NavigationParameter { EntityId = 1 });

        // MainViewModel created the ViewModel and initialized it with the
        // navigation parameter (no view code-behind / App.Services involved).
        Assert.IsType<DictionaryEditViewModel>(_viewModel.CurrentViewModel);
        Assert.Contains("GetByIdAsync:1", _dictionaryService.MethodCalls);
    }

    [Fact]
    public void NavigateToDictionaryEdit_WithoutEntityId_CreatesViewModelForNewDictionary()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        DictionaryEditViewModel vm =
            Assert.IsType<DictionaryEditViewModel>(_viewModel.CurrentViewModel);
        Assert.True(vm.IsNew);
        Assert.DoesNotContain(
            _dictionaryService.MethodCalls,
            call => call.StartsWith("GetByIdAsync:"));
    }
}
#endif
