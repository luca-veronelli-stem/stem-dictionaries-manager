#if WINDOWS
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per VariableEditViewModel.
/// </summary>
public class VariableEditViewModelTests
{
    private readonly MockVariableService _variableService;
    private readonly MockDictionaryService _dictionaryService;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly VariableEditViewModel _viewModel;

    public VariableEditViewModelTests()
    {
        _variableService = new MockVariableService();
        _dictionaryService = new MockDictionaryService();
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        // Seed dizionario non-standard di default (AddressHigh = 0x80)
        _dictionaryService.SeedData(new Dictionary("TestDict", null, isStandard: false));

        _viewModel = new VariableEditViewModel(
            _variableService,
            _dictionaryService,
            _navigationService,
            _dialogService,
            _messageService);
    }

    [Fact]
    public async Task InitializeAsync_WithNull_SetsIsNewTrue()
    {
        // Act
        await _viewModel.InitializeAsync(null, 1);

        // Assert
        Assert.True(_viewModel.IsNew);
        Assert.Equal("Nuova Variabile", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_WithId_SetsIsNewFalse()
    {
        // Arrange
        var variable = new Variable("Existing", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadWrite, "uint16_t");
        _variableService.SeedData(variable);

        // Act
        await _viewModel.InitializeAsync(1, 1);

        // Assert
        Assert.False(_viewModel.IsNew);
        Assert.Equal("Modifica Variabile", _viewModel.FormTitle);
    }

    [Fact]
    public async Task InitializeAsync_LoadsExistingData()
    {
        // Arrange
        var variable = new Variable(
            name: "Temperature",
            addressHigh: 0x80,
            addressLow: 0x10,
            dataTypeKind: DataTypeKind.Int16,
            accessMode: AccessMode.ReadOnly,
            dataTypeRaw: "int16_t",
            isEnabled: true,
            unit: "°C",
            description: "Test var");
        _variableService.SeedData(variable);

        // Act
        await _viewModel.InitializeAsync(1, 1);

        // Assert
        Assert.Equal("Temperature", _viewModel.Name);
        Assert.Equal("80", _viewModel.AddressHighHex);
        Assert.Equal("10", _viewModel.AddressLowHex);
        Assert.Equal(DataTypeKind.Int16, _viewModel.SelectedDataTypeKind);
        Assert.Equal("Int16", _viewModel.DataTypeForSave);
        Assert.Equal(AccessMode.ReadOnly, _viewModel.SelectedAccessMode);
        Assert.Equal("°C", _viewModel.Unit);
        Assert.Equal("Test var", _viewModel.Description);
    }

    [Fact]
    public async Task InitializeAsync_WithNonExistentId_ShowsErrorAndGoesBack()
    {
        // Act
        await _viewModel.InitializeAsync(999, 1);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task InitializeAsync_CanOnlyBeCalledOnce()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _variableService.MethodCalls.Clear();

        // Act
        await _viewModel.InitializeAsync(null, 1);

        // Assert - No additional service calls
        Assert.Empty(_variableService.MethodCalls);
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenNameEmpty()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Desc";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - validation fails, shows warning, does not save
        Assert.True(_viewModel.IsNameInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenDataTypeOtherAndCustomEmpty()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Desc";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Other;
        _viewModel.CustomDataType = "";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsCustomDataTypeInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_CanSave_WhenAllRequiredFieldsSet()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Test description";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public async Task SaveCommand_WhenNew_CallsAddAsync()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 42);
        _viewModel.Name = "NewVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Test description";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls, m => m.StartsWith("AddAsync:42:NewVar"));
    }

    [Fact]
    public async Task SaveCommand_WhenEditing_CallsUpdateAsync()
    {
        // Arrange
        var variable = new Variable("Existing", 0x00, 0x01, DataTypeKind.UInt16, AccessMode.ReadOnly, "uint16_t",
            description: "Existing desc");
        _variableService.SeedData(variable);
        await _viewModel.InitializeAsync(1, 1);
        _viewModel.Name = "UpdatedName";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains("UpdateAsync:1", _variableService.MethodCalls);
    }

    [Fact]
    public async Task SaveCommand_OnSuccess_ShowsMessage_AndGoesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Test description";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Success);
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task SaveCommand_OnError_ShowsErrorDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Test description";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;
        _variableService.ExceptionToThrow = new Exception("Save failed");

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowErrorCalled);
        Assert.False(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CancelCommand_WithNoChanges_GoesBack()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task CancelCommand_WithChanges_ShowsConfirmDialog()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "Changed"; // This sets HasChanges = true
        _viewModel.HasChanges = true;
        _dialogService.ConfirmResult = DialogResult.No;

        // Act
        await _viewModel.CancelCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_dialogService.ShowConfirmCalled);
    }

    [Fact]
    public async Task FullAddressDisplay_FormatsCorrectly()
    {
        // Arrange - dizionario non-standard già seedato (AddressHigh = 0x80)
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.AddressLowHex = "01";

        // Assert
        Assert.Equal("0x8001", _viewModel.FullAddressDisplay);
    }

    [Fact]
    public async Task FullAddressDisplay_HandlesEmptyValues()
    {
        // Dopo InitializeAsync con dizionario non-standard, AddressHigh = 0x80
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        Assert.Equal("0x8000", _viewModel.FullAddressDisplay);
    }

    [Fact]
    public void DataTypeKinds_ContainsAllValues()
    {
        // Assert
        Assert.Equal(Enum.GetValues<DataTypeKind>().Length, _viewModel.DataTypeKinds.Count);
    }

    [Fact]
    public void AccessModes_ContainsAllValues()
    {
        // Assert
        Assert.Equal(Enum.GetValues<AccessMode>().Length, _viewModel.AccessModes.Count);
    }

    #region DataTypeParam Validation Tests

    [Theory]
    [InlineData(DataTypeKind.Bitmapped)]
    [InlineData(DataTypeKind.Array)]
    [InlineData(DataTypeKind.String)]
    public void RequiresDataTypeParam_TrueForParameterizedTypes(DataTypeKind dataType)
    {
        _viewModel.SelectedDataTypeKind = dataType;
        Assert.True(_viewModel.RequiresDataTypeParam);
    }

    [Theory]
    [InlineData(DataTypeKind.UInt8)]
    [InlineData(DataTypeKind.Int16)]
    [InlineData(DataTypeKind.UInt32)]
    [InlineData(DataTypeKind.Other)]
    public void RequiresDataTypeParam_FalseForSimpleTypes(DataTypeKind dataType)
    {
        _viewModel.SelectedDataTypeKind = dataType;
        Assert.False(_viewModel.RequiresDataTypeParam);
    }

    [Fact]
    public void DataTypeParamLabel_Bitmapped_ReturnsWordCountLabel()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        Assert.Contains("Word Count", _viewModel.DataTypeParamLabel);
        Assert.Contains("*", _viewModel.DataTypeParamLabel);
    }

    [Theory]
    [InlineData(DataTypeKind.Array)]
    [InlineData(DataTypeKind.String)]
    public void DataTypeParamLabel_ArrayOrString_ReturnsSizeLabel(DataTypeKind dataType)
    {
        _viewModel.SelectedDataTypeKind = dataType;
        Assert.Contains("Size", _viewModel.DataTypeParamLabel);
        Assert.Contains("*", _viewModel.DataTypeParamLabel);
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenDataTypeParamRequired_AndNotSet()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Desc";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = null;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsDataTypeParamInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public void IsDataTypeParamInvalid_FalseWhenParamSet()
    {
        _viewModel.Name = "TestVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 2;

        Assert.False(_viewModel.IsDataTypeParamInvalid);
    }

    [Fact]
    public void IsDataTypeParamInvalid_FalseWhenNotRequired()
    {
        _viewModel.Name = "TestVar";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        _viewModel.DataTypeParam = null;

        Assert.False(_viewModel.IsDataTypeParamInvalid);
    }

    #endregion

    #region Min/Max Validation Tests

    [Fact]
    public void IsMinMaxValid_TrueWhenBothNull()
    {
        _viewModel.MinValue = null;
        _viewModel.MaxValue = null;
        Assert.True(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public void IsMinMaxValid_TrueWhenOnlyMinSet()
    {
        _viewModel.MinValue = 10;
        _viewModel.MaxValue = null;
        Assert.True(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public void IsMinMaxValid_TrueWhenOnlyMaxSet()
    {
        _viewModel.MinValue = null;
        _viewModel.MaxValue = 100;
        Assert.True(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public void IsMinMaxValid_TrueWhenMinLessThanMax()
    {
        _viewModel.MinValue = 10;
        _viewModel.MaxValue = 100;
        Assert.True(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public void IsMinMaxValid_TrueWhenMinEqualsMax()
    {
        _viewModel.MinValue = 50;
        _viewModel.MaxValue = 50;
        Assert.True(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public void IsMinMaxValid_FalseWhenMinGreaterThanMax()
    {
        _viewModel.MinValue = 100;
        _viewModel.MaxValue = 10;
        Assert.False(_viewModel.IsMinMaxValid);
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenMinMaxInvalid()
    {
        // Arrange
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Desc";
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        _viewModel.MinValue = 100;
        _viewModel.MaxValue = 10;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    #endregion

    #region DataTypeForSave Tests

    [Theory]
    [InlineData(DataTypeKind.UInt8, "UInt8")]
    [InlineData(DataTypeKind.Int16, "Int16")]
    [InlineData(DataTypeKind.UInt32, "UInt32")]
    [InlineData(DataTypeKind.Bitmapped, "Bitmapped")]
    public void DataTypeForSave_ReturnsEnumName_ForStandardTypes(DataTypeKind dataType, string expected)
    {
        _viewModel.SelectedDataTypeKind = dataType;
        Assert.Equal(expected, _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_ReturnsCustomValue_ForOtherType()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Other;
        _viewModel.CustomDataType = "MyCustomStruct";
        Assert.Equal("MyCustomStruct", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_OtherWithEmptyCustom_ReturnsFallback()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Other;
        _viewModel.CustomDataType = "";
        Assert.Equal("Other", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_BitmappedWithParam_IncludesParam()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 2;
        Assert.Equal("Bitmapped[2]", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_StringWithParam_IncludesParam()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.String;
        _viewModel.DataTypeParam = 16;
        Assert.Equal("String[16]", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_ArrayWithParam_IncludesParam()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Array;
        _viewModel.DataTypeParam = 10;
        Assert.Equal("Array[10]", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_SimpleTypeWithoutParam_NoSuffix()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        _viewModel.DataTypeParam = null;
        Assert.Equal("UInt16", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void IsDataTypeOther_TrueOnlyForOther()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Other;
        Assert.True(_viewModel.IsDataTypeOther);

        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt8;
        Assert.False(_viewModel.IsDataTypeOther);
    }

    #endregion

    #region Address Validation Tests

    [Theory]
    [InlineData("00", true)]
    [InlineData("FF", true)]
    [InlineData("", true)]
    [InlineData("XY", false)]
    public void IsAddressLowValid_ValidatesHexInput(string input, bool expected)
    {
        _viewModel.AddressLowHex = input;
        Assert.Equal(expected, _viewModel.IsAddressLowValid);
    }

    #endregion

    #region WordGroups / Bitmapped Tests

    [Fact]
    public void DataTypeParam_WhenBitmapped_GeneratesWordGroups()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 3;

        Assert.Equal(3, _viewModel.WordGroups.Count);
        Assert.Equal("Word 0", _viewModel.WordGroups[0].Label);
        Assert.Equal("Word 1", _viewModel.WordGroups[1].Label);
        Assert.Equal("Word 2", _viewModel.WordGroups[2].Label);
        // Ogni word parte con 1 riga (BitIndex = 0)
        Assert.Single(_viewModel.WordGroups[0].Items);
        Assert.Equal(0, _viewModel.WordGroups[0].Items[0].BitIndex);
    }

    [Fact]
    public void DataTypeParam_WhenNotBitmapped_DoesNotGenerateWordGroups()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.String;
        _viewModel.DataTypeParam = 20;

        Assert.Empty(_viewModel.WordGroups);
    }

    [Fact]
    public void DataTypeParam_ChangingWordCount_RegeneratesGroups()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 2;
        Assert.Equal(2, _viewModel.WordGroups.Count);

        _viewModel.DataTypeParam = 4;
        Assert.Equal(4, _viewModel.WordGroups.Count);
    }

    [Fact]
    public void DataTypeParam_SetToNull_ClearsWordGroups()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 2;
        Assert.NotEmpty(_viewModel.WordGroups);

        _viewModel.DataTypeParam = null;
        Assert.Empty(_viewModel.WordGroups);
    }

    [Fact]
    public void AddBitToWordCommand_AddsBitToCorrectGroup()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 1;
        var group = _viewModel.WordGroups[0];
        Assert.Single(group.Items); // riga iniziale BitIndex=0

        _viewModel.AddBitToWordCommand.Execute(group);

        Assert.Equal(2, group.Items.Count);
        Assert.Equal(1, group.Items[1].BitIndex);
    }

    [Fact]
    public void AddBitToWordCommand_SetsHasChanges()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 1;
        _viewModel.HasChanges = false;

        _viewModel.AddBitToWordCommand.Execute(_viewModel.WordGroups[0]);

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void RemoveBitFromWordCommand_RemovesBitAndSetsHasChanges()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 1;
        var group = _viewModel.WordGroups[0];
        group.TryAddBit(); // ora 2 items
        _viewModel.HasChanges = false;

        _viewModel.RemoveBitFromWordCommand.Execute(group.Items[0]);

        Assert.Single(group.Items);
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveCommand_Bitmapped_CallsUpdateBitInterpretationsAsync()
    {
        // Arrange - dizionario non-standard già seedato (AddressHigh = 0x80)
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.Name = "BitmappedVar";
        // AddressHighHex è computed automaticamente (0x80 per non-standard)
        _viewModel.AddressLowHex = "50";
        _viewModel.Description = "Bitmapped variable";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = 1;
        _viewModel.WordGroups[0].Items[0].Meaning = "Motor";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.Contains(_variableService.MethodCalls,
            m => m.StartsWith("UpdateBitInterpretationsAsync:"));
    }

    #endregion

    #region AddressHigh Computed Tests

    [Fact]
    public void AddressHighHex_BeforeInitialize_DefaultsToNonStandard()
    {
        // Prima di InitializeAsync, _isStandardDictionary è false (default)
        Assert.Equal("80", _viewModel.AddressHighHex);
    }

    [Fact]
    public async Task AddressHighHex_StandardDictionary_Returns00()
    {
        // Arrange
        _dictionaryService.Reset();
        _dictionaryService.SeedData(new Dictionary("Standard", null, isStandard: true));

        var vm = new VariableEditViewModel(
            _variableService, _dictionaryService,
            _navigationService, _dialogService, _messageService);

        // Act
        await vm.InitializeAsync(null, dictionaryId: 1);

        // Assert
        Assert.Equal("00", vm.AddressHighHex);
        Assert.Equal("0x0000", vm.FullAddressDisplay);
    }

    [Fact]
    public async Task AddressHighHex_NonStandardDictionary_Returns80()
    {
        // Act - dizionario non-standard seedato nel ctor
        await _viewModel.InitializeAsync(null, dictionaryId: 1);

        // Assert
        Assert.Equal("80", _viewModel.AddressHighHex);
    }

    [Fact]
    public async Task AddressHighHex_DictionaryNotFound_DefaultsTo80()
    {
        // Arrange - nessun dizionario con ID 999
        // Act
        await _viewModel.InitializeAsync(null, dictionaryId: 999);

        // Assert - fallback: _isStandardDictionary = false → "80"
        Assert.Equal("80", _viewModel.AddressHighHex);
    }

    [Fact]
    public async Task SaveAsync_StandardDictionary_SavesWithAddressHigh00()
    {
        // Arrange
        _dictionaryService.Reset();
        _dictionaryService.SeedData(new Dictionary("Standard", null, isStandard: true));

        var vm = new VariableEditViewModel(
            _variableService, _dictionaryService,
            _navigationService, _dialogService, _messageService);

        await vm.InitializeAsync(null, dictionaryId: 1);
        vm.Name = "StdVar";
        vm.AddressLowHex = "05";
        vm.Description = "Standard variable";
        vm.SelectedDataTypeKind = DataTypeKind.UInt16;

        // Act
        await vm.SaveCommand.ExecuteAsync(null);

        // Assert - verifica che la variabile sia stata creata con AddressHigh=0x00
        Assert.Contains(_variableService.MethodCalls,
            m => m.StartsWith("AddAsync:1:StdVar"));
        Assert.Equal("00", vm.AddressHighHex);
    }

    #endregion

    #region Validation Feedback Tests

    [Fact]
    public void ValidationProperties_FalseBeforeSaveAttempt()
    {
        // Prima di qualsiasi tentativo di salvataggio, nessun campo evidenziato
        _viewModel.Name = "";
        _viewModel.AddressLowHex = "";
        _viewModel.Description = null;

        Assert.False(_viewModel.IsNameInvalid);
        Assert.False(_viewModel.IsAddressLowInvalid);
        Assert.False(_viewModel.IsDescriptionInvalid);
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenAddressLowEmpty()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "";
        _viewModel.Description = "Desc";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsAddressLowInvalid);
        Assert.Contains(_messageService.Messages, m =>
            m.Severity == MessageSeverity.Warning && m.Message.Contains("Indirizzo"));
    }

    [Fact]
    public async Task SaveCommand_Validates_WhenDescriptionEmpty()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsDescriptionInvalid);
        Assert.Contains(_messageService.Messages, m =>
            m.Severity == MessageSeverity.Warning && m.Message.Contains("Descrizione"));
    }

    [Fact]
    public async Task SaveCommand_ValidationClearsAfterFixingFields()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "";
        _viewModel.AddressLowHex = "";
        _viewModel.Description = "";

        // First save attempt → validation fails
        await _viewModel.SaveCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsNameInvalid);
        Assert.True(_viewModel.IsAddressLowInvalid);
        Assert.True(_viewModel.IsDescriptionInvalid);

        // Fix all fields → properties update reactively
        _viewModel.Name = "Fixed";
        Assert.False(_viewModel.IsNameInvalid);
        _viewModel.AddressLowHex = "01";
        Assert.False(_viewModel.IsAddressLowInvalid);
        _viewModel.Description = "Fixed desc";
        Assert.False(_viewModel.IsDescriptionInvalid);
    }

    [Fact]
    public async Task SaveCommand_ValidationMessage_ListsMissingFields()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "";
        _viewModel.AddressLowHex = "";
        _viewModel.Description = "";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var warningMsg = _messageService.Messages.First(m => m.Severity == MessageSeverity.Warning);
        Assert.Contains("Nome", warningMsg.Message);
        Assert.Contains("Indirizzo", warningMsg.Message);
        Assert.Contains("Descrizione", warningMsg.Message);
    }

    [Fact]
    public async Task ValidationProperties_FalseAfterSave_WhenFieldsValid()
    {
        // Arrange - tutti i campi compilati correttamente
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "ValidVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Valid desc";

        // Act - salva (attiva _showValidation ma tutti i campi sono ok)
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert - nessun campo evidenziato in rosso
        Assert.False(_viewModel.IsNameInvalid);
        Assert.False(_viewModel.IsAddressLowInvalid);
        Assert.False(_viewModel.IsDescriptionInvalid);
        Assert.False(_viewModel.IsDataTypeParamInvalid);
        Assert.False(_viewModel.IsCustomDataTypeInvalid);
    }

    [Fact]
    public void IsCustomDataTypeInvalid_FalseWhenNotOther()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        _viewModel.CustomDataType = "";

        Assert.False(_viewModel.IsCustomDataTypeInvalid);
    }

    #endregion
}
#endif
