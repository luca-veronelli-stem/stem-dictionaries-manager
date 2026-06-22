using Core.Enums;
using Core.Models;
using Infrastructure.Entities;

namespace Tests.Shared;

/// <summary>
/// Central source of the magic strings and fixture objects that the test suite
/// repeats across hundreds of call sites (#18). Constants keep the raw
/// <c>dataTypeRaw</c> spellings and client-app names in one place; the factory
/// methods build the common domain/entity fixtures with sensible defaults so a
/// call site only specifies what it actually varies.
/// </summary>
public static class TestData
{
    /// <summary>Canonical <c>dataTypeRaw</c> spellings used across the suite.</summary>
    public static class DataTypes
    {
        public const string UInt8 = "uint8_t";
        public const string UInt16 = "uint16_t";
        public const string UInt32 = "uint32_t";
        public const string String20 = "String[20]";
    }

    /// <summary>Registered client-app identifiers used by the registration/auth tests.</summary>
    public static class ClientApps
    {
        public const string ButtonPanelTester = "ButtonPanelTester";
    }

    /// <summary>Builds a <see cref="Variable"/> with defaults; override only what the test varies.</summary>
    public static Variable CreateVariable(
        string name = "TestVar",
        byte addressHigh = 0x00,
        byte addressLow = 0x01,
        DataTypeKind dataTypeKind = DataTypeKind.UInt8,
        AccessMode accessMode = AccessMode.ReadOnly,
        string dataTypeRaw = DataTypes.UInt8) =>
        new(name, addressHigh, addressLow, dataTypeKind, accessMode, dataTypeRaw);

    /// <summary>Builds a <see cref="Command"/> with defaults; override only what the test varies.</summary>
    public static Command CreateCommand(
        string name = "TestCmd",
        byte codeHigh = 0x00,
        byte codeLow = 0x01,
        bool isResponse = false,
        IEnumerable<string>? parameters = null) =>
        new(name, codeHigh, codeLow, isResponse, parameters);

    /// <summary>Builds a non-admin <see cref="UserEntity"/> fixture.</summary>
    public static UserEntity CreateUser(
        string username = "testuser",
        string displayName = "Test User") =>
        new() { Username = username, DisplayName = displayName };

    /// <summary>Builds the canonical admin <see cref="UserEntity"/> fixture.</summary>
    public static UserEntity CreateAdmin() =>
        new() { Username = "admin", DisplayName = "Admin" };
}
