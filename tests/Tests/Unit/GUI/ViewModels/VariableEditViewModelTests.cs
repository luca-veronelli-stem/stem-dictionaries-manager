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
    [InlineData(DataTypeKind.Bitmapped)]
    public void RequiresDataTypeParam_FalseForSimpleTypes(DataTypeKind dataType)
    {
        _viewModel.SelectedDataTypeKind = dataType;
        Assert.False(_viewModel.RequiresDataTypeParam);
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
        // Arrange — Array richiede DataTypeParam via TextBox
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "TestVar";
        _viewModel.AddressLowHex = "01";
        _viewModel.Description = "Desc";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Array;
        _viewModel.DataTypeParam = null;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_viewModel.IsDataTypeParamInvalid);
        Assert.Contains(_messageService.Messages, m => m.Severity == MessageSeverity.Warning);
        Assert.DoesNotContain(_variableService.MethodCalls, m => m.StartsWith("AddAsync"));
    }

    [Fact]
    public void IsDataTypeParamInvalid_FalseForBitmapped()
    {
        // Bitmapped non usa più DataTypeParam TextBox
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.DataTypeParam = null;

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
    public void DataTypeForSave_BitmappedWithWords_IncludesWordCount()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        Assert.Equal("Bitmapped[2]", _viewModel.DataTypeForSave);
    }

    [Fact]
    public void DataTypeForSave_BitmappedSingleWord_ShowsOne()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.Equal("Bitmapped[1]", _viewModel.DataTypeForSave);
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
    public void SelectingBitmapped_AutoCreatesWord0()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        Assert.Single(_viewModel.WordGroups);
        Assert.Equal("Word 0", _viewModel.WordGroups[0].Label);
        Assert.Single(_viewModel.WordGroups[0].Items);
        Assert.Equal(0, _viewModel.WordGroups[0].Items[0].BitIndex);
    }

    [Fact]
    public void SelectingNonBitmapped_ClearsWordGroups()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.NotEmpty(_viewModel.WordGroups);

        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        Assert.Empty(_viewModel.WordGroups);
    }

    [Fact]
    public void AddBitToWordCommand_AddsBitToCorrectGroup()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        var group = _viewModel.WordGroups[0];
        Assert.Single(group.Items);

        _viewModel.AddBitToWordCommand.Execute(group);

        Assert.Equal(2, group.Items.Count);
        Assert.Equal(1, group.Items[1].BitIndex);
    }

    [Fact]
    public void AddBitToWordCommand_SetsHasChanges()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.HasChanges = false;

        _viewModel.AddBitToWordCommand.Execute(_viewModel.WordGroups[0]);

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void RemoveBitFromWordCommand_RemovesBitAndSetsHasChanges()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        var group = _viewModel.WordGroups[0];
        group.TryAddBit();
        _viewModel.HasChanges = false;

        _viewModel.RemoveBitFromWordCommand.Execute(group.Items[0]);

        Assert.Single(group.Items);
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public async Task SaveCommand_Bitmapped_CallsUpdateBitInterpretationsAsync()
    {
        await _viewModel.InitializeAsync(null, dictionaryId: 1);
        _viewModel.Name = "BitmappedVar";
        _viewModel.AddressLowHex = "50";
        _viewModel.Description = "Bitmapped variable";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.WordGroups[0].Items[0].Meaning = "Motor";

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Contains(_variableService.MethodCalls,
            m => m.StartsWith("UpdateBitInterpretationsAsync:"));
    }

    [Fact]
    public void AddWordCommand_AddsNewWordGroup()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.Single(_viewModel.WordGroups);

        _viewModel.AddWordCommand.Execute(null);

        Assert.Equal(2, _viewModel.WordGroups.Count);
        Assert.Equal("Word 1", _viewModel.WordGroups[1].Label);
        Assert.Single(_viewModel.WordGroups[1].Items);
        Assert.Equal(0, _viewModel.WordGroups[1].Items[0].BitIndex);
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void AddWordCommand_MultipleWords()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.AddWordCommand.Execute(null);

        Assert.Equal(3, _viewModel.WordGroups.Count);
        Assert.Equal("Bitmapped[3]", _viewModel.DataTypeForSave);
    }

    [Fact]
    public async Task RemoveWordCommand_RemovesWord_AndReindexes()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.AddWordCommand.Execute(null);
        Assert.Equal(3, _viewModel.WordGroups.Count);

        // Rimuovi Word 1 (mezzo)
        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[1]);

        Assert.Equal(2, _viewModel.WordGroups.Count);
        Assert.Equal("Word 0", _viewModel.WordGroups[0].Label);
        Assert.Equal("Word 1", _viewModel.WordGroups[1].Label);
    }

    [Fact]
    public async Task RemoveWordCommand_WithNonEmptyMeanings_ShowsConfirmDialog()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.WordGroups[1].Items[0].Meaning = "Motor Active";
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[1]);

        Assert.True(_dialogService.ShowConfirmCalled);
        Assert.Single(_viewModel.WordGroups);
    }

    [Fact]
    public async Task RemoveWordCommand_ConfirmNo_DoesNotRemove()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.WordGroups[1].Items[0].Meaning = "Motor Active";
        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[1]);

        Assert.Equal(2, _viewModel.WordGroups.Count);
    }

    [Fact]
    public void CanRemoveWord_FalseWhenSingleWord()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.Single(_viewModel.WordGroups);
        Assert.False(_viewModel.CanRemoveWord);
    }

    [Fact]
    public void CanRemoveWord_TrueWhenMultipleWords()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        Assert.True(_viewModel.CanRemoveWord);
    }

    [Fact]
    public async Task RemoveWordCommand_WhenOnlyOneWord_DoesNothing()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        var singleGroup = _viewModel.WordGroups[0];

        await _viewModel.RemoveWordCommand.ExecuteAsync(singleGroup);

        Assert.Single(_viewModel.WordGroups);
    }

    [Fact]
    public async Task RemoveWordCommand_ReindexesItemsWordIndex()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.AddWordCommand.Execute(null);
        // Word 2 items have WordIndex = 2
        Assert.Equal(2, _viewModel.WordGroups[2].Items[0].WordIndex);

        // Rimuovi Word 0
        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[0]);

        // Ex-Word 2 è ora Word 1
        Assert.Equal(1, _viewModel.WordGroups[1].Items[0].WordIndex);
    }

    [Fact]
    public void RemoveLastBitFromWordCommand_RemovesLastBit()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        var group = _viewModel.WordGroups[0];
        _viewModel.AddBitToWordCommand.Execute(group);
        _viewModel.AddBitToWordCommand.Execute(group);
        Assert.Equal(3, group.Items.Count);

        _viewModel.RemoveLastBitFromWordCommand.Execute(group);

        Assert.Equal(2, group.Items.Count);
        Assert.Equal(1, group.Items[^1].BitIndex);
        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void RemoveLastBitFromWordCommand_WhenSingleBit_DoesNothing()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        var group = _viewModel.WordGroups[0];
        Assert.Single(group.Items);

        _viewModel.RemoveLastBitFromWordCommand.Execute(group);

        Assert.Single(group.Items);
    }

    [Fact]
    public async Task RemoveWordCommand_UpdatesDataTypeForSave()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.AddWordCommand.Execute(null);
        Assert.Equal("Bitmapped[3]", _viewModel.DataTypeForSave);

        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[2]);

        Assert.Equal("Bitmapped[2]", _viewModel.DataTypeForSave);
    }

    [Fact]
    public async Task RemoveWordCommand_EmptyMeanings_DoesNotShowDialog()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        // Word 1 ha meanings vuoti (default)

        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[1]);

        Assert.False(_dialogService.ShowConfirmCalled);
        Assert.Single(_viewModel.WordGroups);
    }

    [Fact]
    public async Task RemoveWordCommand_SetsHasChanges()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        _viewModel.AddWordCommand.Execute(null);
        _viewModel.HasChanges = false;

        await _viewModel.RemoveWordCommand.ExecuteAsync(_viewModel.WordGroups[1]);

        Assert.True(_viewModel.HasChanges);
    }

    [Fact]
    public void SwitchingBitmappedToOtherAndBack_RecreatesWord0()
    {
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.Single(_viewModel.WordGroups);
        _viewModel.WordGroups[0].Items[0].Meaning = "Motor";

        // Switch away — resets WordSize and WordGroups
        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;
        Assert.Empty(_viewModel.WordGroups);
        Assert.Null(_viewModel.SelectedWordSize);

        // Switch back — need to set WordSize again
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.Single(_viewModel.WordGroups);
        Assert.Equal("Word 0", _viewModel.WordGroups[0].Label);
        Assert.Single(_viewModel.WordGroups[0].Items);
        // Meaning è vuoto (Word 0 ricreata da zero)
        Assert.Equal(string.Empty, _viewModel.WordGroups[0].Items[0].Meaning);
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

    #region DictionaryContext Mode Tests (v7)

    [Fact]
    public async Task InitializeAsync_WithDictionaryContextId_SetsDictionaryContextMode()
    {
        // Arrange
        SeedBitmappedVariable();

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Assert
        Assert.True(_viewModel.IsDictionaryContext);
        Assert.False(_viewModel.IsNotDictionaryContext);
    }

    [Fact]
    public async Task InitializeAsync_WithoutDictionaryContextId_SetsNormalMode()
    {
        await _viewModel.InitializeAsync(variableId: null, dictionaryId: 1);

        Assert.False(_viewModel.IsDictionaryContext);
        Assert.True(_viewModel.IsNotDictionaryContext);
    }

    [Fact]
    public async Task FormTitle_InDictionaryContext_ReturnsDictionaryTitle()
    {
        SeedBitmappedVariable();

        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        Assert.Equal("Override Variabile Standard", _viewModel.FormTitle);
    }

    [Fact]
    public async Task SaveButtonLabel_InDictionaryContext_ReturnsSalvaBit()
    {
        SeedBitmappedVariable();

        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        Assert.Contains("Salva Override", _viewModel.SaveButtonLabel);
    }

    [Fact]
    public async Task SaveButtonLabel_InNormalMode_ReturnsSalva()
    {
        await _viewModel.InitializeAsync(variableId: null, dictionaryId: 1);

        Assert.EndsWith("Salva", _viewModel.SaveButtonLabel);
        Assert.DoesNotContain("Bit", _viewModel.SaveButtonLabel);
    }

    [Fact]
    public async Task DictionaryContext_Save_CallsUpdateBitInterpretationsForDictionaryAsync()
    {
        // Arrange
        SeedBitmappedVariable();
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Modifica un bit
        _viewModel.WordGroups[0].Items[0].Meaning = "Dictionary override";

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert — deve usare il metodo per-dizionario
        Assert.Contains(_variableService.MethodCalls,
            m => m.StartsWith("UpdateBitInterpretationsForDictionaryAsync:1:42"));
    }

    [Fact]
    public async Task DictionaryContext_Save_DoesNotCallUpdateAsync()
    {
        // Arrange
        SeedBitmappedVariable();
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert — non deve salvare la variabile stessa
        Assert.DoesNotContain(_variableService.MethodCalls,
            m => m.StartsWith("UpdateAsync:"));
        Assert.DoesNotContain(_variableService.MethodCalls,
            m => m.StartsWith("AddAsync:"));
    }

    [Fact]
    public async Task DictionaryContext_Save_DoesNotValidateVariableFields()
    {
        // Arrange — variabile bitmapped caricata, campi nome/desc sono vuoti (read-only in GUI)
        SeedBitmappedVariable();
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Act — salva senza errori (campi variabile non validati in DictionaryContext)
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert — navigazione GoBack avvenuta (salvataggio riuscito)
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task DictionaryContext_LoadsBitsForDictionary()
    {
        // Arrange
        SeedBitmappedVariable();
        _variableService.SeedBitInterpretations(1,
        [
            new BitInterpretation(1, 0, 0, "Dictionary bit 0", dictionaryId: 42),
            new BitInterpretation(1, 0, 1, "Common bit 1", dictionaryId: null),
        ]);

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Assert — deve chiamare GetBitInterpretationsForDictionaryAsync
        Assert.Contains(_variableService.MethodCalls,
            m => m == "GetBitInterpretationsForDictionaryAsync:1:42");
    }

    [Fact]
    public async Task NormalMode_LoadsBitsWithGetBitInterpretationsAsync()
    {
        // Arrange
        SeedBitmappedVariable();

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1);

        // Assert — deve usare il metodo classico (senza dizionario)
        Assert.Contains(_variableService.MethodCalls,
            m => m == "GetBitInterpretationsAsync:1");
    }

    [Fact]
    public async Task DictionaryContext_Save_ShowsSuccessMessage()
    {
        // Arrange
        SeedBitmappedVariable();
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert — salva override dizionario, mostra messaggio successo
        Assert.Contains("dizionario", _messageService.CurrentMessage ?? "",
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DictionaryContext_Save_CallsSetOverrideAsync()
    {
        // Arrange
        SeedBitmappedVariable();
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);
        _viewModel.IsEnabled = false;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert — deve salvare l'override per dizionario
        Assert.Contains(_variableService.MethodCalls,
            m => m == "SetOverrideAsync:42:1:False:Status flags");
    }

    [Fact]
    public async Task DictionaryContext_LoadsOverride()
    {
        // Arrange — override esistente: disabilitata per dizionario 42
        SeedBitmappedVariable();
        _variableService.SeedOverrides(
            StandardVariableOverride.Restore(1, 42, 1, isEnabled: false, description: null));

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Assert
        Assert.False(_viewModel.IsEnabled);
    }

    [Fact]
    public async Task DictionaryContext_NoOverride_DefaultsToEnabled()
    {
        // Arrange — nessun override per dizionario 42
        SeedBitmappedVariable();

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Assert — default = true
        Assert.True(_viewModel.IsEnabled);
    }

    [Fact]
    public async Task DictionaryContext_LoadsOverride_WithDescription()
    {
        // Arrange — override con description personalizzata
        SeedBitmappedVariable();
        _variableService.SeedOverrides(
            StandardVariableOverride.Restore(1, 42, 1, isEnabled: false, description: "Override desc"));

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Assert
        Assert.False(_viewModel.IsEnabled);
        Assert.Equal("Override desc", _viewModel.Description);
    }

    [Fact]
    public async Task DictionaryContext_NoOverride_KeepsTemplateDescription()
    {
        // Arrange — nessun override: Description resta quella del template
        SeedBitmappedVariable(); // template ha "Status flags"

        // Act
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);

        // Assert
        Assert.Equal("Status flags", _viewModel.Description);
    }

    [Fact]
    public async Task DictionaryContext_Save_PassesDescriptionToSetOverride()
    {
        // Arrange
        SeedBitmappedVariable();
        await _viewModel.InitializeAsync(variableId: 1, dictionaryId: 1, dictionaryContextId: 42);
        _viewModel.Description = "Nuova descrizione dizionario";
        _viewModel.IsEnabled = true;

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert — verifica che description venga passata
        Assert.Contains(_variableService.MethodCalls,
            m => m == "SetOverrideAsync:42:1:True:Nuova descrizione dizionario");
    }

    private void SeedBitmappedVariable()
    {
        var bitmapped = Variable.Restore(
            id: 1, name: "StatusBits", addressHigh: 0x00, addressLow: 0x10,
            dataTypeKind: DataTypeKind.Bitmapped, dataTypeRaw: "bitmapped[1]",
            dataTypeParam: 1, accessMode: AccessMode.ReadOnly,
            isEnabled: true, format: null, minValue: null, maxValue: null,
            unit: null, usage: null, description: "Status flags",
            wordSize: 16);
        _variableService.SeedData(bitmapped);
    }

    #endregion

    #region WordSize (BR-019)

    [Fact]
    public async Task SelectedWordSize_NullByDefault()
    {
        await _viewModel.InitializeAsync(null, 1);

        Assert.Null(_viewModel.SelectedWordSize);
    }

    [Fact]
    public async Task IsBitmapped_ShowsWordSizeSelector()
    {
        await _viewModel.InitializeAsync(null, 1);

        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;

        Assert.True(_viewModel.IsBitmapped);
        Assert.False(_viewModel.HasWordSize);
    }

    [Fact]
    public async Task HasWordSize_TrueAfterSelectingWordSize()
    {
        await _viewModel.InitializeAsync(null, 1);

        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        Assert.True(_viewModel.HasWordSize);
    }

    [Fact]
    public async Task SelectedWordSize_CreatesWordGroupsWhenSet()
    {
        await _viewModel.InitializeAsync(null, 1);

        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        Assert.Empty(_viewModel.WordGroups);

        _viewModel.SelectedWordSize = 8;

        Assert.Single(_viewModel.WordGroups);
        Assert.Equal(8, _viewModel.WordGroups[0].MaxBitsPerWord);
    }

    [Fact]
    public async Task ChangingDataTypeKind_ResetsWordSize()
    {
        await _viewModel.InitializeAsync(null, 1);

        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;
        Assert.True(_viewModel.HasWordSize);

        _viewModel.SelectedDataTypeKind = DataTypeKind.UInt16;

        Assert.Null(_viewModel.SelectedWordSize);
        Assert.False(_viewModel.HasWordSize);
    }

    [Fact]
    public async Task WordSizeOptions_Contains_8_16_32()
    {
        await _viewModel.InitializeAsync(null, 1);

        Assert.Equal([8, 16, 32], _viewModel.WordSizeOptions);
    }

    [Fact]
    public async Task Validate_MissingWordSize_ShowsWarning()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "Test";
        _viewModel.AddressLowHex = "06";
        _viewModel.Description = "Allarmi";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        // SelectedWordSize left null

        await _viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsWordSizeInvalid);
    }

    [Fact]
    public async Task LoadExisting_BitmappedWithWordSize_LoadsWordSize()
    {
        SeedBitmappedVariable();
        _dictionaryService.SeedData(
            new Dictionary("Standard", "Standard vars", isStandard: true));

        await _viewModel.InitializeAsync(1, 2);

        Assert.Equal(16, _viewModel.SelectedWordSize);
        Assert.True(_viewModel.HasWordSize);
    }

    [Fact]
    public async Task SaveNew_Bitmapped_IncludesWordSize()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.Name = "Allarmi";
        _viewModel.AddressLowHex = "06";
        _viewModel.Description = "Allarmi bitmapped";
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 32;

        await _viewModel.SaveCommand.ExecuteAsync(null);

        var saved = _variableService.GetSavedVariable();
        Assert.NotNull(saved);
        Assert.Equal(32, saved.WordSize);
    }

    [Fact]
    public async Task ReduceWordSize_TruncatesOverflowBits()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        // Aggiungi 10 bit alla Word 0
        var group = _viewModel.WordGroups[0];
        for (var i = 0; i < 9; i++)
            group.TryAddBit();
        Assert.Equal(10, group.Items.Count);

        // Riduci a 8 — i bit 8 e 9 devono sparire
        _viewModel.SelectedWordSize = 8;

        Assert.Equal(8, _viewModel.WordGroups[0].Items.Count);
        Assert.True(_viewModel.WordGroups[0].Items.All(i => i.BitIndex < 8));
    }

    [Fact]
    public async Task ReduceWordSize_WithNonEmptyMeanings_ShowsConfirm()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        // Aggiungi bit e metti un meaning al bit 10
        var group = _viewModel.WordGroups[0];
        for (var i = 0; i < 10; i++)
            group.TryAddBit();
        group.Items[10].Meaning = "Overflow alarm";

        _dialogService.ConfirmResult = DialogResult.Yes;

        // Riduci a 8
        _viewModel.SelectedWordSize = 8;

        // Deve attendere il dialog async
        await Task.Delay(50);

        Assert.True(_dialogService.ShowConfirmCalled);
        Assert.Equal(8, _viewModel.WordGroups[0].Items.Count);
    }

    [Fact]
    public async Task ReduceWordSize_ConfirmNo_RestoresPreviousValue()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        var group = _viewModel.WordGroups[0];
        for (var i = 0; i < 10; i++)
            group.TryAddBit();
        group.Items[10].Meaning = "Alarm";

        _dialogService.ConfirmResult = DialogResult.No;

        // Riduci a 8 — utente dice No
        _viewModel.SelectedWordSize = 8;

        await Task.Delay(50);

        Assert.True(_dialogService.ShowConfirmCalled);
        // WordSize ripristinato a 16
        Assert.Equal(16, _viewModel.SelectedWordSize);
        // Bit non troncati
        Assert.Equal(11, _viewModel.WordGroups[0].Items.Count);
    }

    [Fact]
    public async Task ReduceWordSize_NoOverflow_NoConfirm()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        // Solo 3 bit — tutti sotto 8
        var group = _viewModel.WordGroups[0];
        group.TryAddBit();
        group.TryAddBit();
        Assert.Equal(3, group.Items.Count);

        // Riduci a 8 — nessun overflow
        _viewModel.SelectedWordSize = 8;

        Assert.False(_dialogService.ShowConfirmCalled);
        Assert.Equal(3, _viewModel.WordGroups[0].Items.Count);
        Assert.Equal(8, _viewModel.WordGroups[0].MaxBitsPerWord);
    }

    [Fact]
    public async Task IncreaseWordSize_KeepsExistingBits()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 8;

        // Aggiungi 5 bit (totale 6) alla Word 0
        var group = _viewModel.WordGroups[0];
        for (var i = 0; i < 5; i++)
            group.TryAddBit();
        Assert.Equal(6, group.Items.Count);
        group.Items[3].Meaning = "Important";

        // Aumenta a 16
        _viewModel.SelectedWordSize = 16;

        // Bit esistenti preservati, MaxBitsPerWord aggiornato
        Assert.Equal(6, _viewModel.WordGroups[0].Items.Count);
        Assert.Equal(16, _viewModel.WordGroups[0].MaxBitsPerWord);
        Assert.Equal("Important", _viewModel.WordGroups[0].Items[3].Meaning);
        Assert.False(_dialogService.ShowConfirmCalled);
    }

    [Fact]
    public async Task ReduceWordSize_EmptyMeanings_NoConfirm_StillTruncates()
    {
        await _viewModel.InitializeAsync(null, 1);
        _viewModel.SelectedDataTypeKind = DataTypeKind.Bitmapped;
        _viewModel.SelectedWordSize = 16;

        // Aggiungi 10 bit alla Word 0 — tutti con meaning vuoto
        var group = _viewModel.WordGroups[0];
        for (var i = 0; i < 9; i++)
            group.TryAddBit();
        Assert.Equal(10, group.Items.Count);

        // Riduci a 8 — overflow ma senza meanings
        _viewModel.SelectedWordSize = 8;

        // Serve delay per l'async in HandleWordSizeReductionAsync
        await Task.Delay(50);

        // Nessun dialog (meanings vuoti) ma bit troncati
        Assert.False(_dialogService.ShowConfirmCalled);
        Assert.Equal(8, _viewModel.WordGroups[0].Items.Count);
        Assert.True(_viewModel.WordGroups[0].Items.All(i => i.BitIndex < 8));
    }

    #endregion
}
#endif
