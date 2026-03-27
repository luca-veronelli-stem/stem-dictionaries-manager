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
        var result = services.AddInfrastructure(TestConnectionString);

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
        var provider = services.BuildServiceProvider();

        // Assert
        var context = provider.GetService<AppDbContext>();
        Assert.NotNull(context);
    }

    [Fact]
    public void AddInfrastructure_RegistersUserRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IUserRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersBoardRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IBoardRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersDictionaryRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IDictionaryRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersVariableRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IVariableRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersCommandRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ICommandRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersAuditEntryRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IAuditEntryRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersBitInterpretationRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IBitInterpretationRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersCommandDeviceStateRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<ICommandDeviceStateRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersVariableDeviceStateRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Assert
        var repository = provider.GetService<IVariableDeviceStateRepository>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddInfrastructure_RegistersRepositoriesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Act - Create two scopes and verify different instances
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var repo1 = scope1.ServiceProvider.GetService<IUserRepository>();
        var repo2 = scope2.ServiceProvider.GetService<IUserRepository>();

        // Assert - Different scopes should have different instances
        Assert.NotSame(repo1, repo2);
    }

    [Fact]
    public void AddInfrastructure_SameScopeReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        // Act - Get same service twice from same scope
        using var scope = provider.CreateScope();
        var repo1 = scope.ServiceProvider.GetService<IUserRepository>();
        var repo2 = scope.ServiceProvider.GetService<IUserRepository>();

        // Assert - Same scope should return same instance
        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void AddInfrastructure_RegistersDeviceRepository()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(TestConnectionString);
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetService<IDeviceRepository>();

        Assert.NotNull(repository);
    }
}
