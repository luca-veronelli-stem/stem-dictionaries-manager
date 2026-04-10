using API.Mapping;
using Core.Enums;
using Core.Models;

namespace Tests.Unit.API;

/// <summary>
/// Unit tests per ApiMapper — mapping domain models → DTO API.
/// </summary>
public class ApiMapperTests
{
    // === Variable → VariableDto ===

    [Fact]
    public void ToVariableDto_MapsAllProperties()
    {
        var variable = Variable.Restore(
            id: 1, name: "Firmware", addressHigh: 0x00, addressLow: 0x01,
            dataTypeKind: DataTypeKind.UInt16, dataTypeRaw: "UInt16",
            dataTypeParam: null, accessMode: AccessMode.ReadOnly,
            isEnabled: true, format: null, minValue: 0, maxValue: 65535,
            unit: "V", usage: null, description: "Firmware versione",
            wordSize: null);

        var dto = ApiMapper.ToVariableDto(variable);

        Assert.Equal("Firmware", dto.Name);
        Assert.Equal(0x00, dto.AddressHigh);
        Assert.Equal(0x01, dto.AddressLow);
        Assert.Equal("UInt16", dto.DataType);
        Assert.Equal("ReadOnly", dto.Access);
        Assert.Equal("Firmware versione", dto.Description);
        Assert.Equal(0, dto.Min);
        Assert.Equal(65535, dto.Max);
        Assert.Equal("V", dto.Unit);
    }

    [Fact]
    public void ToVariableDto_NullOptionalFields_AreNull()
    {
        var variable = Variable.Restore(
            id: 2, name: "Test", addressHigh: 0x80, addressLow: 0x05,
            dataTypeKind: DataTypeKind.UInt8, dataTypeRaw: "UInt8",
            dataTypeParam: null, accessMode: AccessMode.ReadWrite,
            isEnabled: true, format: null, minValue: null, maxValue: null,
            unit: null, usage: null, description: null, wordSize: null);

        var dto = ApiMapper.ToVariableDto(variable);

        Assert.Null(dto.Description);
        Assert.Null(dto.Min);
        Assert.Null(dto.Max);
        Assert.Null(dto.Unit);
    }

    // === Variable → ResolvedVariableDto ===

    [Fact]
    public void ToResolvedDto_Standard_SetsIsStandardTrue()
    {
        var variable = Variable.Restore(
            id: 1, name: "Firmware", addressHigh: 0x00, addressLow: 0x01,
            dataTypeKind: DataTypeKind.UInt16, dataTypeRaw: "UInt16",
            dataTypeParam: null, accessMode: AccessMode.ReadOnly,
            isEnabled: true, format: null, minValue: null, maxValue: null,
            unit: null, usage: null, description: "Template desc",
            wordSize: null);

        var dto = ApiMapper.ToResolvedDto(variable, isStandard: true);

        Assert.True(dto.IsStandard);
        Assert.Equal("Template desc", dto.Description);
    }

    [Fact]
    public void ToResolvedDto_WithOverrideDescription_UsesOverride()
    {
        var variable = Variable.Restore(
            id: 1, name: "Firmware", addressHigh: 0x00, addressLow: 0x01,
            dataTypeKind: DataTypeKind.UInt16, dataTypeRaw: "UInt16",
            dataTypeParam: null, accessMode: AccessMode.ReadOnly,
            isEnabled: true, format: null, minValue: null, maxValue: null,
            unit: null, usage: null, description: "Template desc",
            wordSize: null);

        var dto = ApiMapper.ToResolvedDto(variable, isStandard: true,
            overrideDescription: "Override desc");

        Assert.Equal("Override desc", dto.Description);
    }

    [Fact]
    public void ToResolvedDto_Specific_SetsIsStandardFalse()
    {
        var variable = Variable.Restore(
            id: 5, name: "SystemOn", addressHigh: 0x80, addressLow: 0x03,
            dataTypeKind: DataTypeKind.UInt8, dataTypeRaw: "UInt8",
            dataTypeParam: null, accessMode: AccessMode.ReadOnly,
            isEnabled: true, format: null, minValue: 0, maxValue: 1,
            unit: null, usage: null, description: "Piano acceso",
            wordSize: null);

        var dto = ApiMapper.ToResolvedDto(variable, isStandard: false);

        Assert.False(dto.IsStandard);
    }

    // === Device → DeviceSummaryDto ===

    [Fact]
    public void ToDeviceSummaryDto_MapsAllProperties()
    {
        var device = Device.Restore(1, "Optimus-XP", 10, "Piano oleodinamico");

        var dto = ApiMapper.ToDeviceSummaryDto(device);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Optimus-XP", dto.Name);
        Assert.Equal(10, dto.MachineCode);
        Assert.Equal("Piano oleodinamico", dto.Description);
    }

    [Fact]
    public void ToDeviceSummaryDto_NullDescription_MapsNull()
    {
        var device = Device.Restore(2, "Spark", 7, null);

        var dto = ApiMapper.ToDeviceSummaryDto(device);

        Assert.Null(dto.Description);
    }

    // === Board → BoardSummaryDto ===

    [Fact]
    public void ToBoardSummaryDto_MapsAllProperties()
    {
        var board = Board.Restore(
            id: 1, deviceId: 3, name: "Madre", firmwareType: 5,
            boardNumber: 1, partNumber: "DIS0020477", isPrimary: true,
            dictionaryId: 10, dictionaryName: "Eden-XP",
            deviceName: "Eden-XP", machineCode: 3);

        var dto = ApiMapper.ToBoardSummaryDto(board);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Madre", dto.Name);
        Assert.True(dto.IsPrimary);
        Assert.Equal(5, dto.FirmwareType);
        Assert.Equal(1, dto.BoardNumber);
        Assert.StartsWith("0x", dto.ProtocolAddress);
        Assert.Equal(10, dto.DictionaryId);
        Assert.Equal("Eden-XP", dto.DictionaryName);
    }

    [Fact]
    public void ToBoardSummaryDto_NullDictionary_MapsNull()
    {
        var board = Board.Restore(
            id: 2, deviceId: 7, name: "Motore DX", firmwareType: 12,
            boardNumber: 2, partNumber: null, isPrimary: false,
            dictionaryId: null, dictionaryName: null);

        var dto = ApiMapper.ToBoardSummaryDto(board);

        Assert.Null(dto.DictionaryId);
        Assert.Null(dto.DictionaryName);
    }

    // === Dictionary → DictionarySummaryDto ===

    [Fact]
    public void ToDictionarySummaryDto_MapsAllProperties()
    {
        var dict = Core.Models.Dictionary.Restore(
            1, "Standard", "Variabili comuni", true, []);

        var dto = ApiMapper.ToDictionarySummaryDto(dict, variableCount: 24);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Standard", dto.Name);
        Assert.Equal("Variabili comuni", dto.Description);
        Assert.True(dto.IsStandard);
        Assert.Equal(24, dto.VariableCount);
    }

    // === Command → CommandDto ===

    [Fact]
    public void ToCommandDto_MapsAllProperties()
    {
        var command = Command.Restore(1, "Read Variable", 0x00, 0x01,
            false, ["4|size", "2|address"]);

        var dto = ApiMapper.ToCommandDto(command);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Read Variable", dto.Name);
        Assert.Equal(0x00, dto.CodeHigh);
        Assert.Equal(0x01, dto.CodeLow);
        Assert.False(dto.IsResponse);
        Assert.Equal(2, dto.Parameters.Count);
    }

    // === Command → CommandDeviceDto ===

    [Fact]
    public void ToCommandDeviceDto_MapsAllProperties()
    {
        var command = Command.Restore(1, "Write Variable", 0x00, 0x02,
            false, ["4|size"]);

        var dto = ApiMapper.ToCommandDeviceDto(command);

        Assert.Equal("Write Variable", dto.Name);
        Assert.Equal(0x00, dto.CodeHigh);
        Assert.Equal(0x02, dto.CodeLow);
        Assert.False(dto.IsResponse);
        Assert.Single(dto.Parameters);
    }

    [Fact]
    public void ToCommandDeviceDto_Response_IsResponseTrue()
    {
        var command = Command.Restore(2, "Read Response", 0x80, 0x01,
            true, []);

        var dto = ApiMapper.ToCommandDeviceDto(command);

        Assert.True(dto.IsResponse);
        Assert.Equal(0x80, dto.CodeHigh);
    }
}
