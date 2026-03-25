using Core.Enums;
using Infrastructure.Entities;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.Infrastructure;

/// <summary>
/// Test per scenari CRUD avanzati: Update, Delete con cascade,
/// violazioni di constraint, e audit trail completo.
/// </summary>
public class CrudScenariosTests : IntegrationTestBase
{
    #region Update Scenarios

    [Fact]
    public async Task UpdateAsync_User_ModifiesDisplayName()
    {
        // Arrange
        var user = new UserEntity { Username = "testuser", DisplayName = "Old Name" };
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        var originalCreatedAt = user.CreatedAt;

        // Act
        user.DisplayName = "New Name";
        Context.Users.Update(user);
        await Context.SaveChangesAsync();

        // Assert
        var updated = await Context.Users.FindAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.DisplayName);
        Assert.Equal(originalCreatedAt, updated.CreatedAt);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_Dictionary_PreservesVariables()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "TestDict", Description = "Original" };
        var variable = new VariableEntity
        {
            Name = "TestVar",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint8_t",
            Dictionary = dictionary
        };
        await Context.Dictionaries.AddAsync(dictionary);
        await Context.Variables.AddAsync(variable);
        await Context.SaveChangesAsync();

        // Act
        dictionary.Description = "Updated";
        Context.Dictionaries.Update(dictionary);
        await Context.SaveChangesAsync();

        // Assert
        var updated = await Context.Dictionaries
            .Include(d => d.Variables)
            .FirstOrDefaultAsync(d => d.Id == dictionary.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.Description);
        Assert.Single(updated.Variables);
        Assert.Equal("TestVar", updated.Variables.First().Name);
    }

    [Fact]
    public async Task UpdateAsync_Variable_SetsUpdatedAt()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "TestDict" };
        var variable = new VariableEntity
        {
            Name = "OriginalName",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint8_t",
            Dictionary = dictionary
        };
        await Context.Dictionaries.AddAsync(dictionary);
        await Context.Variables.AddAsync(variable);
        await Context.SaveChangesAsync();
        Assert.Null(variable.UpdatedAt);

        // Act
        variable.Name = "UpdatedName";
        Context.Variables.Update(variable);
        await Context.SaveChangesAsync();

        // Assert
        var updated = await Context.Variables.FindAsync(variable.Id);
        Assert.NotNull(updated);
        Assert.Equal("UpdatedName", updated.Name);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_Command_ModifiesParameters()
    {
        // Arrange
        var command = new CommandEntity
        {
            Name = "TestCommand",
            CodeHigh = 0x01,
            CodeLow = 0x02,
            IsResponse = false,
            ParametersJson = "{\"old\": true}"
        };
        await Context.Commands.AddAsync(command);
        await Context.SaveChangesAsync();

        // Act
        command.ParametersJson = "{\"new\": true}";
        Context.Commands.Update(command);
        await Context.SaveChangesAsync();

        // Assert
        var updated = await Context.Commands.FindAsync(command.Id);
        Assert.NotNull(updated);
        Assert.Equal("{\"new\": true}", updated.ParametersJson);
        Assert.NotNull(updated.UpdatedAt);
    }

    #endregion

    #region Delete with Cascade Scenarios

    [Fact]
    public async Task DeleteDictionary_CascadesDeleteToVariables()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "ToDelete" };
        var variable1 = new VariableEntity
        {
            Name = "Var1",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint8_t",
            Dictionary = dictionary
        };
        var variable2 = new VariableEntity
        {
            Name = "Var2",
            AddressHigh = 0x00,
            AddressLow = 0x02,
            DataTypeKind = DataTypeKind.UInt16,
            AccessMode = AccessMode.ReadWrite,
            DataTypeRaw = "uint16_t",
            Dictionary = dictionary
        };
        await Context.Dictionaries.AddAsync(dictionary);
        await Context.Variables.AddRangeAsync(variable1, variable2);
        await Context.SaveChangesAsync();
        var dictId = dictionary.Id;
        var var1Id = variable1.Id;
        var var2Id = variable2.Id;

        // Act
        Context.Dictionaries.Remove(dictionary);
        await Context.SaveChangesAsync();

        // Assert
        Assert.Null(await Context.Dictionaries.FindAsync(dictId));
        Assert.Null(await Context.Variables.FindAsync(var1Id));
        Assert.Null(await Context.Variables.FindAsync(var2Id));
        Assert.Empty(await Context.Variables.Where(v => v.DictionaryId == dictId).ToListAsync());
    }

    [Fact]
    public async Task DeleteVariable_CascadesDeleteToBitInterpretations()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "TestDict" };
        var variable = new VariableEntity
        {
            Name = "BitmappedVar",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.Bitmapped,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint16_t",
            Dictionary = dictionary
        };
        var bit1 = new BitInterpretationEntity
        {
            WordIndex = 0,
            BitIndex = 0,
            Meaning = "Bit 0",
            Variable = variable
        };
        var bit2 = new BitInterpretationEntity
        {
            WordIndex = 0,
            BitIndex = 1,
            Meaning = "Bit 1",
            Variable = variable
        };
        await Context.Dictionaries.AddAsync(dictionary);
        await Context.Variables.AddAsync(variable);
        await Context.BitInterpretations.AddRangeAsync(bit1, bit2);
        await Context.SaveChangesAsync();
        var varId = variable.Id;

        // Act
        Context.Variables.Remove(variable);
        await Context.SaveChangesAsync();

        // Assert
        Assert.Null(await Context.Variables.FindAsync(varId));
        Assert.Empty(await Context.BitInterpretations.Where(b => b.VariableId == varId).ToListAsync());
    }

    [Fact]
    public async Task DeleteCommand_CascadesDeleteToDeviceStates()
    {
        // Arrange
        var command = new CommandEntity
        {
            Name = "TestCommand",
            CodeHigh = 0x10,
            CodeLow = 0x20,
            IsResponse = false
        };
        var state1 = new CommandDeviceStateEntity
        {
            DeviceType = DeviceType.OptimusXp,
            IsEnabled = true,
            Command = command
        };
        var state2 = new CommandDeviceStateEntity
        {
            DeviceType = DeviceType.EdenXp,
            IsEnabled = false,
            Command = command
        };
        await Context.Commands.AddAsync(command);
        await Context.CommandDeviceStates.AddRangeAsync(state1, state2);
        await Context.SaveChangesAsync();
        var cmdId = command.Id;

        // Act
        Context.Commands.Remove(command);
        await Context.SaveChangesAsync();

        // Assert
        Assert.Null(await Context.Commands.FindAsync(cmdId));
        Assert.Empty(await Context.CommandDeviceStates.Where(s => s.CommandId == cmdId).ToListAsync());
    }

    #endregion

    #region Unique Constraint Violations

    [Fact]
    public async Task AddUser_DuplicateUsername_ThrowsDbUpdateException()
    {
        // Arrange
        var user1 = new UserEntity { Username = "duplicate", DisplayName = "User 1" };
        await Context.Users.AddAsync(user1);
        await Context.SaveChangesAsync();

        // Act & Assert
        var user2 = new UserEntity { Username = "duplicate", DisplayName = "User 2" };
        await Context.Users.AddAsync(user2);
        await Assert.ThrowsAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddDictionary_DuplicateName_ThrowsDbUpdateException()
    {
        // Arrange
        var dict1 = new DictionaryEntity { Name = "SameName" };
        await Context.Dictionaries.AddAsync(dict1);
        await Context.SaveChangesAsync();

        // Act & Assert
        var dict2 = new DictionaryEntity { Name = "SameName" };
        await Context.Dictionaries.AddAsync(dict2);
        await Assert.ThrowsAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddVariable_DuplicateAddressInSameDictionary_ThrowsDbUpdateException()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "TestDict" };
        var var1 = new VariableEntity
        {
            Name = "Var1",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint8_t",
            Dictionary = dictionary
        };
        await Context.Dictionaries.AddAsync(dictionary);
        await Context.Variables.AddAsync(var1);
        await Context.SaveChangesAsync();

        // Act & Assert - Same address in same dictionary
        var var2 = new VariableEntity
        {
            Name = "Var2",
            AddressHigh = 0x00,
            AddressLow = 0x01, // Same address!
            DataTypeKind = DataTypeKind.UInt16,
            AccessMode = AccessMode.ReadWrite,
            DataTypeRaw = "uint16_t",
            DictionaryId = dictionary.Id
        };
        await Context.Variables.AddAsync(var2);
        await Assert.ThrowsAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddVariable_SameAddressDifferentDictionary_Succeeds()
    {
        // Arrange
        var dict1 = new DictionaryEntity { Name = "Dict1" };
        var dict2 = new DictionaryEntity { Name = "Dict2" };
        await Context.Dictionaries.AddRangeAsync(dict1, dict2);
        await Context.SaveChangesAsync();

        // Act - Same address but different dictionaries
        var var1 = new VariableEntity
        {
            Name = "Var1",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint8_t",
            DictionaryId = dict1.Id
        };
        var var2 = new VariableEntity
        {
            Name = "Var2",
            AddressHigh = 0x00,
            AddressLow = 0x01, // Same address!
            DataTypeKind = DataTypeKind.UInt16,
            AccessMode = AccessMode.ReadWrite,
            DataTypeRaw = "uint16_t",
            DictionaryId = dict2.Id
        };
        await Context.Variables.AddRangeAsync(var1, var2);
        await Context.SaveChangesAsync();

        // Assert - Both should exist
        Assert.Equal(2, await Context.Variables.CountAsync());
    }

    [Fact]
    public async Task AddCommand_DuplicateCode_ThrowsDbUpdateException()
    {
        // Arrange
        var cmd1 = new CommandEntity
        {
            Name = "Command1",
            CodeHigh = 0x01,
            CodeLow = 0x02,
            IsResponse = false
        };
        await Context.Commands.AddAsync(cmd1);
        await Context.SaveChangesAsync();

        // Act & Assert - Same code and IsResponse
        var cmd2 = new CommandEntity
        {
            Name = "Command2",
            CodeHigh = 0x01,
            CodeLow = 0x02,
            IsResponse = false
        };
        await Context.Commands.AddAsync(cmd2);
        await Assert.ThrowsAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddCommand_SameCodeDifferentIsResponse_Succeeds()
    {
        // Arrange & Act - Same code but different IsResponse
        var cmdRequest = new CommandEntity
        {
            Name = "Request",
            CodeHigh = 0x01,
            CodeLow = 0x02,
            IsResponse = false
        };
        var cmdResponse = new CommandEntity
        {
            Name = "Response",
            CodeHigh = 0x01,
            CodeLow = 0x02,
            IsResponse = true // Different IsResponse
        };
        await Context.Commands.AddRangeAsync(cmdRequest, cmdResponse);
        await Context.SaveChangesAsync();

        // Assert - Both should exist
        Assert.Equal(2, await Context.Commands.CountAsync());
    }

    #endregion

    #region Audit Trail Complete Lifecycle

    [Fact]
    public async Task AuditTrail_CreateUpdateDelete_TracksAllChanges()
    {
        // Arrange & Act - Create
        var user = new UserEntity { Username = "audituser", DisplayName = "Initial" };
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        var createdAt = user.CreatedAt;
        Assert.NotEqual(default, createdAt);
        Assert.Null(user.UpdatedAt);

        // Act - Update
        await Task.Delay(10); // Ensure time difference
        user.DisplayName = "Updated";
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        var updatedAt = user.UpdatedAt;
        Assert.NotNull(updatedAt);
        Assert.True(updatedAt > createdAt);

        // Act - Delete
        var userId = user.Id;
        Context.Users.Remove(user);
        await Context.SaveChangesAsync();

        // Assert - User is deleted
        Assert.Null(await Context.Users.FindAsync(userId));
    }

    [Fact]
    public async Task AuditTrail_MultipleUpdates_UpdatesTimestampEachTime()
    {
        // Arrange
        var dictionary = new DictionaryEntity { Name = "AuditDict", Description = "V1" };
        await Context.Dictionaries.AddAsync(dictionary);
        await Context.SaveChangesAsync();
        var createdAt = dictionary.CreatedAt;

        // Act - First update
        await Task.Delay(10);
        dictionary.Description = "V2";
        Context.Dictionaries.Update(dictionary);
        await Context.SaveChangesAsync();
        var firstUpdate = dictionary.UpdatedAt;

        // Act - Second update
        await Task.Delay(10);
        dictionary.Description = "V3";
        Context.Dictionaries.Update(dictionary);
        await Context.SaveChangesAsync();
        var secondUpdate = dictionary.UpdatedAt;

        // Assert
        Assert.NotNull(firstUpdate);
        Assert.NotNull(secondUpdate);
        Assert.True(firstUpdate > createdAt);
        Assert.True(secondUpdate > firstUpdate);
    }

    #endregion

    #region Repository Update/Delete via Repository Pattern

    [Fact]
    public async Task UserRepository_UpdateAsync_ModifiesEntity()
    {
        // Arrange
        var repository = new UserRepository(Context);
        var user = new UserEntity { Username = "repouser", DisplayName = "Original" };
        await repository.AddAsync(user);

        // Act
        user.DisplayName = "Modified";
        await repository.UpdateAsync(user);

        // Assert
        var updated = await repository.GetByIdAsync(user.Id);
        Assert.NotNull(updated);
        Assert.Equal("Modified", updated.DisplayName);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task DictionaryRepository_DeleteAsync_RemovesCascadedVariables()
    {
        // Arrange
        var dictRepository = new DictionaryRepository(Context);
        var varRepository = new VariableRepository(Context);

        var dictionary = new DictionaryEntity { Name = "RepoDictToDelete" };
        await dictRepository.AddAsync(dictionary);

        var variable = new VariableEntity
        {
            Name = "RepoVar",
            AddressHigh = 0x00,
            AddressLow = 0x01,
            DataTypeKind = DataTypeKind.UInt8,
            AccessMode = AccessMode.ReadOnly,
            DataTypeRaw = "uint8_t",
            DictionaryId = dictionary.Id
        };
        await varRepository.AddAsync(variable);
        var dictId = dictionary.Id;
        var varId = variable.Id;

        // Act
        await dictRepository.DeleteAsync(dictId);

        // Assert
        Assert.Null(await dictRepository.GetByIdAsync(dictId));
        Assert.Null(await varRepository.GetByIdAsync(varId));
    }

    #endregion
}
