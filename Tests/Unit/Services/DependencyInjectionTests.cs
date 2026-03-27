using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Services;
using Services.Interfaces;

namespace Tests.Unit.Services;

/// <summary>
/// Test per la registrazione DI di Services.
/// </summary>
public class DependencyInjectionTests
{
    private const string TestConnectionString = "Data Source=:memory:";

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        services.AddServices();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddServices_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);

        // Act
        var result = services.AddServices();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddServices_RegistersDictionaryService()
    {
        // Arrange & Act
        var provider = BuildServiceProvider();

        // Assert
        var service = provider.GetService<IDictionaryService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersVariableService()
    {
        // Arrange & Act
        var provider = BuildServiceProvider();

        // Assert
        var service = provider.GetService<IVariableService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersCommandService()
    {
        // Arrange & Act
        var provider = BuildServiceProvider();

        // Assert
        var service = provider.GetService<ICommandService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersBoardService()
    {
        // Arrange & Act
        var provider = BuildServiceProvider();

        // Assert
        var service = provider.GetService<IBoardService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersUserService()
    {
        // Arrange & Act
        var provider = BuildServiceProvider();

        // Assert
        var service = provider.GetService<IUserService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersDeviceService()
    {
        var provider = BuildServiceProvider();

        var service = provider.GetService<IDeviceService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersServicesAsScoped()
    {
        // Arrange
        var provider = BuildServiceProvider();

        // Act - Create two scopes and verify different instances
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var service1 = scope1.ServiceProvider.GetService<IDictionaryService>();
        var service2 = scope2.ServiceProvider.GetService<IDictionaryService>();

        // Assert - Different scopes should have different instances
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddServices_SameScopeReturnsSameInstance()
    {
        // Arrange
        var provider = BuildServiceProvider();

        // Act - Get same service twice from same scope
        using var scope = provider.CreateScope();
        var service1 = scope.ServiceProvider.GetService<IDictionaryService>();
        var service2 = scope.ServiceProvider.GetService<IDictionaryService>();

        // Assert - Same scope should return same instance
        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddServices_WithoutInfrastructure_ThrowsOnResolve()
    {
        // Arrange - Only register Services, not Infrastructure
        var services = new ServiceCollection();
        services.AddServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert - Should throw because dependencies are missing
        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IDictionaryService>());
    }

    [Fact]
    public void AddServices_AllServicesResolvable()
    {
        // Arrange
        var provider = BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Act & Assert - All services should resolve without exception
        Assert.NotNull(sp.GetRequiredService<IDictionaryService>());
        Assert.NotNull(sp.GetRequiredService<IVariableService>());
        Assert.NotNull(sp.GetRequiredService<ICommandService>());
        Assert.NotNull(sp.GetRequiredService<IBoardService>());
        Assert.NotNull(sp.GetRequiredService<IUserService>());
    }
}
