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
        IServiceCollection result = services.AddServices();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddServices_RegistersDictionaryService()
    {
        // Arrange & Act
        ServiceProvider provider = BuildServiceProvider();

        // Assert
        IDictionaryService? service = provider.GetService<IDictionaryService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersVariableService()
    {
        // Arrange & Act
        ServiceProvider provider = BuildServiceProvider();

        // Assert
        IVariableService? service = provider.GetService<IVariableService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersCommandService()
    {
        // Arrange & Act
        ServiceProvider provider = BuildServiceProvider();

        // Assert
        ICommandService? service = provider.GetService<ICommandService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersBoardService()
    {
        // Arrange & Act
        ServiceProvider provider = BuildServiceProvider();

        // Assert
        IBoardService? service = provider.GetService<IBoardService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersUserService()
    {
        // Arrange & Act
        ServiceProvider provider = BuildServiceProvider();

        // Assert
        IUserService? service = provider.GetService<IUserService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersDeviceService()
    {
        ServiceProvider provider = BuildServiceProvider();

        IDeviceService? service = provider.GetService<IDeviceService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersAuditService()
    {
        ServiceProvider provider = BuildServiceProvider();

        IAuditService? service = provider.GetService<IAuditService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddServices_RegistersServicesAsScoped()
    {
        // Arrange
        ServiceProvider provider = BuildServiceProvider();

        // Act - Create two scopes and verify different instances
        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();
        IDictionaryService? service1 = scope1.ServiceProvider.GetService<IDictionaryService>();
        IDictionaryService? service2 = scope2.ServiceProvider.GetService<IDictionaryService>();

        // Assert - Different scopes should have different instances
        Assert.NotSame(service1, service2);
    }

    [Fact]
    public void AddServices_SameScopeReturnsSameInstance()
    {
        // Arrange
        ServiceProvider provider = BuildServiceProvider();

        // Act - Get same service twice from same scope
        using IServiceScope scope = provider.CreateScope();
        IDictionaryService? service1 = scope.ServiceProvider.GetService<IDictionaryService>();
        IDictionaryService? service2 = scope.ServiceProvider.GetService<IDictionaryService>();

        // Assert - Same scope should return same instance
        Assert.Same(service1, service2);
    }

    [Fact]
    public void AddServices_WithoutInfrastructure_ThrowsImmediately()
    {
        // Arrange - Infrastructure not registered
        var services = new ServiceCollection();

        // Act & Assert - AddServices fails fast at registration time with a
        // clear message, instead of an opaque error at DI-resolution time.
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
            () => services.AddServices());
        Assert.Contains("AddInfrastructure", ex.Message);
    }

    [Fact]
    public void AddServices_AllServicesResolvable()
    {
        // Arrange
        ServiceProvider provider = BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IServiceProvider sp = scope.ServiceProvider;

        // Act & Assert - All services should resolve without exception
        Assert.NotNull(sp.GetRequiredService<IDictionaryService>());
        Assert.NotNull(sp.GetRequiredService<IVariableService>());
        Assert.NotNull(sp.GetRequiredService<ICommandService>());
        Assert.NotNull(sp.GetRequiredService<IBoardService>());
        Assert.NotNull(sp.GetRequiredService<IUserService>());
        Assert.NotNull(sp.GetRequiredService<IAuditService>());
    }
}
