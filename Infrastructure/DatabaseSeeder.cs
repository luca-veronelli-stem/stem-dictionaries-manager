using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Popola il database con dati di esempio per sviluppo/demo.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Popola il database con dati di esempio se è vuoto.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        // Se ci sono già dati, non fare nulla
        if (await context.Users.AnyAsync())
            return;

        // === Users ===
        var users = new[]
        {
            new UserEntity { Username = "admin", DisplayName = "Amministratore" },
            new UserEntity { Username = "luca", DisplayName = "Luca Rossi" },
            new UserEntity { Username = "marco", DisplayName = "Marco Bianchi" }
        };
        context.Users.AddRange(users);

        // === BoardTypes ===
        var boardTypes = new[]
        {
            new BoardTypeEntity { Name = "Madre Optimus", FirmwareType = 17 },
            new BoardTypeEntity { Name = "Madre Eden", FirmwareType = 18 },
            new BoardTypeEntity { Name = "Pulsantiera 4x4", FirmwareType = 4 },
            new BoardTypeEntity { Name = "Pulsantiera 8x8", FirmwareType = 8 },
            new BoardTypeEntity { Name = "Sherpa Slim", FirmwareType = 20 },
            new BoardTypeEntity { Name = "Display LCD", FirmwareType = 10 }
        };
        context.BoardTypes.AddRange(boardTypes);
        await context.SaveChangesAsync();

        // === Dictionaries ===
        var dictStandard = new DictionaryEntity
        {
            Name = "Standard",
            Description = "Variabili comuni a tutti i dispositivi",
            BoardTypeId = null  // Standard = senza BoardType
        };

        var dictOptimus = new DictionaryEntity
        {
            Name = "Optimus",
            Description = "Variabili specifiche per schede madre Optimus",
            BoardTypeId = boardTypes[0].Id
        };

        var dictEden = new DictionaryEntity
        {
            Name = "Eden",
            Description = "Variabili specifiche per schede madre Eden",
            BoardTypeId = boardTypes[1].Id
        };

        var dictPulsantiere = new DictionaryEntity
        {
            Name = "Pulsantiere",
            Description = "Variabili per tastiere e pulsantiere",
            BoardTypeId = boardTypes[2].Id
        };

        context.Dictionaries.AddRange(dictStandard, dictOptimus, dictEden, dictPulsantiere);
        await context.SaveChangesAsync();

        // === Variables per Standard ===
        var standardVars = new[]
        {
            CreateVariable(dictStandard.Id, "Firmware Version", 0x00, 0x01, "UInt16", "Versione firmware"),
            CreateVariable(dictStandard.Id, "Serial Number", 0x00, 0x02, "String[16]", "Numero seriale"),
            CreateVariable(dictStandard.Id, "Device Status", 0x00, 0x10, "Bitmapped", "Stato dispositivo"),
            CreateVariable(dictStandard.Id, "Error Code", 0x00, 0x11, "UInt16", "Codice errore"),
            CreateVariable(dictStandard.Id, "Uptime", 0x00, 0x20, "UInt32", "Tempo di attività in secondi"),
        };
        context.Variables.AddRange(standardVars);

        // === Variables per Optimus ===
        var optimusVars = new[]
        {
            CreateVariable(dictOptimus.Id, "Temperature CPU", 0x80, 0x01, "Int16", "Temperatura CPU in decimi di grado"),
            CreateVariable(dictOptimus.Id, "Fan Speed", 0x80, 0x02, "UInt16", "Velocità ventola RPM"),
            CreateVariable(dictOptimus.Id, "Power Mode", 0x80, 0x10, "UInt8", "Modalità alimentazione"),
            CreateVariable(dictOptimus.Id, "Relay Status", 0x80, 0x20, "Bitmapped", "Stato relè"),
        };
        context.Variables.AddRange(optimusVars);

        // === Variables per Pulsantiere ===
        var pulsantiereVars = new[]
        {
            CreateVariable(dictPulsantiere.Id, "Button State", 0x80, 0x01, "Bitmapped", "Stato pulsanti"),
            CreateVariable(dictPulsantiere.Id, "LED State", 0x80, 0x02, "Bitmapped", "Stato LED"),
            CreateVariable(dictPulsantiere.Id, "Backlight", 0x80, 0x10, "UInt8", "Luminosità retroilluminazione"),
        };
        context.Variables.AddRange(pulsantiereVars);

        await context.SaveChangesAsync();

        // === Commands ===
        var commands = new[]
        {
            new CommandEntity
            {
                Name = "Read Variable",
                CodeHigh = 0x01,
                CodeLow = 0x00,
                IsResponse = false,
                ParametersJson = "[{\"name\":\"address\",\"type\":\"UInt16\"}]"
            },
            new CommandEntity
            {
                Name = "Read Variable Response",
                CodeHigh = 0x01,
                CodeLow = 0x00,
                IsResponse = true,
                ParametersJson = "[{\"name\":\"address\",\"type\":\"UInt16\"},{\"name\":\"value\",\"type\":\"ByteArray\"}]"
            },
            new CommandEntity
            {
                Name = "Write Variable",
                CodeHigh = 0x02,
                CodeLow = 0x00,
                IsResponse = false,
                ParametersJson = "[{\"name\":\"address\",\"type\":\"UInt16\"},{\"name\":\"value\",\"type\":\"ByteArray\"}]"
            },
            new CommandEntity
            {
                Name = "Write Variable Response",
                CodeHigh = 0x02,
                CodeLow = 0x00,
                IsResponse = true,
                ParametersJson = "[{\"name\":\"address\",\"type\":\"UInt16\"},{\"name\":\"result\",\"type\":\"UInt8\"}]"
            },
            new CommandEntity
            {
                Name = "Reset Device",
                CodeHigh = 0x10,
                CodeLow = 0x00,
                IsResponse = false,
                ParametersJson = "[]"
            },
        };
        context.Commands.AddRange(commands);

        await context.SaveChangesAsync();
    }

    private static VariableEntity CreateVariable(
        int dictionaryId,
        string name,
        byte addressHigh,
        byte addressLow,
        string dataType,
        string? description = null)
    {
        return new VariableEntity
        {
            DictionaryId = dictionaryId,
            Name = name,
            AddressHigh = addressHigh,
            AddressLow = addressLow,
            DataTypeKind = ParseDataTypeKind(dataType),
            DataTypeParam = ParseDataTypeParam(dataType),
            DataTypeRaw = dataType,
            Description = description,
            AccessMode = Core.Enums.AccessMode.ReadWrite,
            IsEnabled = true
        };
    }

    private static Core.Enums.DataTypeKind ParseDataTypeKind(string dataType)
    {
        if (dataType.StartsWith("String")) return Core.Enums.DataTypeKind.String;
        if (dataType.StartsWith("Array")) return Core.Enums.DataTypeKind.Array;
        
        return dataType switch
        {
            "UInt8" => Core.Enums.DataTypeKind.UInt8,
            "Int8" => Core.Enums.DataTypeKind.Int8,
            "UInt16" => Core.Enums.DataTypeKind.UInt16,
            "Int16" => Core.Enums.DataTypeKind.Int16,
            "UInt32" => Core.Enums.DataTypeKind.UInt32,
            "Int32" => Core.Enums.DataTypeKind.Int32,
            "Bitmapped" => Core.Enums.DataTypeKind.Bitmapped,
            _ => Core.Enums.DataTypeKind.Other
        };
    }

    private static int? ParseDataTypeParam(string dataType)
    {
        // Estrae il parametro da tipi come "String[16]" o "Array[32]"
        var start = dataType.IndexOf('[');
        var end = dataType.IndexOf(']');
        
        if (start > 0 && end > start)
        {
            var param = dataType.Substring(start + 1, end - start - 1);
            if (int.TryParse(param, out var value))
                return value;
        }
        
        return null;
    }
}
