using API.Dtos;
using Core.Models;

namespace API.Mapping;

/// <summary>
/// Maps domain models to API DTOs.
/// </summary>
public static class ApiMapper
{
    public static VariableDto ToVariableDto(Variable v) => new(
        Name: v.Name,
        AddressHigh: v.AddressHigh,
        AddressLow: v.AddressLow,
        DataType: v.DataTypeRaw,
        Access: v.AccessMode.ToString(),
        Description: v.Description,
        Min: v.MinValue,
        Max: v.MaxValue,
        Unit: v.Unit);

    public static ResolvedVariableDto ToResolvedDto(Variable v, bool isStandard,
        string? overrideDescription = null) => new(
        Name: v.Name,
        AddressHigh: v.AddressHigh,
        AddressLow: v.AddressLow,
        DataType: v.DataTypeRaw,
        Access: v.AccessMode.ToString(),
        Description: overrideDescription ?? v.Description,
        Min: v.MinValue,
        Max: v.MaxValue,
        Unit: v.Unit,
        IsStandard: isStandard);

    public static DeviceSummaryDto ToDeviceSummaryDto(Device d) => new(
        Id: d.Id,
        Name: d.Name,
        MachineCode: d.MachineCode,
        Description: d.Description);

    public static BoardSummaryDto ToBoardSummaryDto(Board b) => new(
        Id: b.Id,
        Name: b.Name,
        IsPrimary: b.IsPrimary,
        FirmwareType: b.FirmwareType,
        BoardNumber: b.BoardNumber,
        ProtocolAddress: $"0x{b.ProtocolAddress:X8}",
        DictionaryId: b.DictionaryId,
        DictionaryName: b.DictionaryName);

    public static DictionarySummaryDto ToDictionarySummaryDto(
        Dictionary d, int variableCount) => new(
        Id: d.Id,
        Name: d.Name,
        Description: d.Description,
        IsStandard: d.IsStandard,
        VariableCount: variableCount);

    public static CommandDto ToCommandDto(Command c) => new(
        Id: c.Id,
        Name: c.Name,
        CodeHigh: c.CodeHigh,
        CodeLow: c.CodeLow,
        IsResponse: c.IsResponse,
        Parameters: [.. c.Parameters]);

    public static CommandDeviceDto ToCommandDeviceDto(Command c) => new(
        Name: c.Name,
        CodeHigh: c.CodeHigh,
        CodeLow: c.CodeLow,
        IsResponse: c.IsResponse,
        Parameters: [.. c.Parameters]);
}
