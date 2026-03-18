#if WINDOWS
using GUI.Windows;
using GUI.Windows.Abstractions;
using GUI.Windows.Services;
using GUI.Windows.ViewModels;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services;

namespace Tests.Unit.GUI;

/// <summary>
/// Test per la registrazione DI del layer GUI.
/// </summary>
public class DependencyInjectionTests
{
    private readonly IServiceCollection _services;

    public DependencyInjectionTests()
    {
        _services = new ServiceCollection();
        
        // Setup prerequisites (Infrastructure + Services)
        _services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=:memory:"));
        _services.AddInfrastructure("Data Source=:memory:");
        _services.AddServices();
        
        // Add GUI
        _services.AddGUI();
    }

    [Fact]
    public void AddGUI_RegistersNavigationService()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var service = provider.GetService<INavigationService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<NavigationService>(service);
    }

    [Fact]
    public void AddGUI_RegistersDialogService()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var service = provider.GetService<IDialogService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<DialogService>(service);
    }

    [Fact]
    public void AddGUI_RegistersMessageService()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var service = provider.GetService<IMessageService>();

        // Assert
        Assert.NotNull(service);
        Assert.IsType<MessageService>(service);
    }

    [Fact]
    public void AddGUI_RegistersMainViewModel()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var viewModel = provider.GetService<MainViewModel>();

        // Assert
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void AddGUI_RegistersDictionaryListViewModel()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var viewModel = provider.GetService<DictionaryListViewModel>();

        // Assert
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void AddGUI_RegistersDictionaryEditViewModel()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var viewModel = provider.GetService<DictionaryEditViewModel>();

        // Assert
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void NavigationService_IsSingleton()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var service1 = provider.GetService<INavigationService>();
        var service2 = provider.GetService<INavigationService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    public void DialogService_IsSingleton()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var service1 = provider.GetService<IDialogService>();
        var service2 = provider.GetService<IDialogService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    public void MessageService_IsSingleton()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var service1 = provider.GetService<IMessageService>();
        var service2 = provider.GetService<IMessageService>();

        // Assert
        Assert.Same(service1, service2);
    }

    [Fact]
    public void ViewModels_AreTransient()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var vm1 = provider.GetService<DictionaryListViewModel>();
        var vm2 = provider.GetService<DictionaryListViewModel>();

        // Assert
        Assert.NotSame(vm1, vm2);
    }

    [Fact]
    public void ViewModels_ReceiveCorrectDependencies()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var viewModel = provider.GetService<DictionaryListViewModel>();

        // Assert - ViewModel should be created without exceptions
        Assert.NotNull(viewModel);
    }

    [Fact]
    public void MainViewModel_ReceivesAllDependencies()
    {
        // Act
        var provider = _services.BuildServiceProvider();
        var viewModel = provider.GetService<MainViewModel>();

        // Assert
        Assert.NotNull(viewModel);
        // MainViewModel should set initial title (with suffix for initial view)
        Assert.StartsWith("Stem Dictionaries Manager", viewModel.Title);
    }
}
#endif
