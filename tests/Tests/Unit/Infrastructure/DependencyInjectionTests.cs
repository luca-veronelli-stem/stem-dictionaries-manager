using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Unit.Infrastructure;

/// <summary>
/// Test per la registrazione DI di Infrastructure.
/// </summary>
public class DependencyInjectionTests
{
    private const string TestConnectionString = "Data Source=:memory:";

    [Fact]
    public void AddInfrastructure_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        IServiceCollection result = services.AddInfrastructure(TestConnectionString);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddInfrastructure_RegistersAppDbContext()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        AppDbContext? context = provider.GetService<AppDbContext>();
        Assert.NotNull(context);
    }

    [Fact]
    public void AddInfrastructure_RegistersUserRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IUserRepository? repository = provider.GetService<IUserRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersBoardRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IBoardRepository? repository = provider.GetService<IBoardRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersDictionaryRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IDictionaryRepository? repository = provider.GetService<IDictionaryRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersVariableRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IVariableRepository? repository = provider.GetService<IVariableRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersCommandRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        ICommandRepository? repository = provider.GetService<ICommandRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersAuditEntryRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IAuditEntryRepository? repository = provider.GetService<IAuditEntryRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersBitInterpretationRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IBitInterpretationRepository? repository = provider.GetService<IBitInterpretationRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersCommandDeviceStateRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        ICommandDeviceStateRepository? repository = provider.GetService<ICommandDeviceStateRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersVariableDeviceStateRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IStandardVariableOverrideRepository? repository = provider.GetService<IStandardVariableOverrideRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersRepositoriesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Act - Create two scopes and verify different instances
        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();
        IUserRepository? repo1 = scope1.ServiceProvider.GetService<IUserRepository>();
        IUserRepository? repo2 = scope2.ServiceProvider.GetService<IUserRepository>();

        // Assert - Different scopes should have different instances
        Assert.NotSame(repo1, repo2);
    }

    [Fact]
    public void AddInfrastructure_SameScopeReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        // Act - Get same service twice from same scope
        using IServiceScope scope = provider.CreateScope();
        IUserRepository? repo1 = scope.ServiceProvider.GetService<IUserRepository>();
        IUserRepository? repo2 = scope.ServiceProvider.GetService<IUserRepository>();

        // Assert - Same scope should return same instance
        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void AddInfrastructure_RegistersDeviceRepository()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IDeviceRepository? repository = scope.ServiceProvider.GetService<IDeviceRepository>();

        Assert.NotNull(repository);
    }
}
