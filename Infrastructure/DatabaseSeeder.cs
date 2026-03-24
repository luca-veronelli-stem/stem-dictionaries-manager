using Core.Enums;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Popola il database con dati di esempio per sviluppo/demo.
/// Domain v2: Board→Dictionary diretto, nessun BoardType.
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
            new UserEntity { Username = "michele.pignedoli", DisplayName = "Michele Pignedoli" },
            new UserEntity { Username = "lorenzo.vecchi", DisplayName = "Lorenzo Vecchi" },
            new UserEntity { Username = "andrea.acunzo", DisplayName = "Andrea Acunzo" },
            new UserEntity { Username = "alessandro.goldoni", DisplayName = "Alessandro Goldoni" },
            new UserEntity { Username = "luca.veronelli", DisplayName = "Luca Veronelli" }
        };
        context.Users.AddRange(users);

        // === Dictionaries ===
        var dictStandard = new DictionaryEntity
        {
            Name = "Standard",
            Description = "Variabili comuni a tutti i dispositivi STEM",
            IsStandard = true
        };
        var dictOptimusXp = new DictionaryEntity
        {
            Name = "Optimus XP",
            Description = "Variabili specifiche schede madre Optimus XP (FW Type 17)"
        };
        var dictEdenXp = new DictionaryEntity
        {
            Name = "Eden XP",
            Description = "Variabili specifiche schede madre Eden XP (FW Type 18)"
        };
        var dictPulsantiere = new DictionaryEntity
        {
            Name = "Pulsantiere",
            Description = "Variabili per tastiere e pulsantiere (condiviso tra device)"
        };
        var dictMotore = new DictionaryEntity
        {
            Name = "Driver Motore",
            Description = "Variabili per controllo motori"
        };
        var dictR3lMaster = new DictionaryEntity
        {
            Name = "R3L-XP Master",
            Description = "Variabili specifiche R3L-XP scheda Master (FW Type 11)"
        };
        var dictR3lSlave = new DictionaryEntity
        {
            Name = "R3L-XP Slave",
            Description = "Variabili specifiche R3L-XP scheda Slave (FW Type 12)"
        };
        var dictSpark = new DictionaryEntity
        {
            Name = "Spark",
            Description = "Variabili specifiche Spark HMI (FW Type 20)"
        };

        context.Dictionaries.AddRange(dictStandard, dictOptimusXp, dictEdenXp,
            dictPulsantiere, dictMotore, dictR3lMaster, dictR3lSlave, dictSpark);
        await context.SaveChangesAsync();

        // === Boards ===
        // OptimusXp
        var boards = new List<BoardEntity>
        {
            CreateBoard(DeviceType.OptimusXp, "Madre Master", 17, 1, "DIS0100001",
                isPrimary: true, dictionaryId: dictOptimusXp.Id),
            CreateBoard(DeviceType.OptimusXp, "Pulsantiera 1", 4, 1, "DIS0100010",
                dictionaryId: dictPulsantiere.Id),
            CreateBoard(DeviceType.OptimusXp, "Pulsantiera 2", 5, 2, "DIS0100011",
                dictionaryId: dictPulsantiere.Id),

            // EdenXp
            CreateBoard(DeviceType.EdenXp, "Madre", 18, 1, "DIS0030001",
                isPrimary: true, dictionaryId: dictEdenXp.Id),
            CreateBoard(DeviceType.EdenXp, "Pulsantiera 1", 4, 1, "DIS0030010",
                dictionaryId: dictPulsantiere.Id),
            CreateBoard(DeviceType.EdenXp, "Driver Motore", 25, 1, "DIS0030030",
                dictionaryId: dictMotore.Id),

            // SherpaSlim
            CreateBoard(DeviceType.SherpaSlim, "Madre", 20, 1, "DIS0010001",
                isPrimary: true),

            // R3L-XP (2 board con dizionari diversi)
            CreateBoard(DeviceType.R3lXp, "Master", 11, 1, "DIS0110001",
                isPrimary: true, dictionaryId: dictR3lMaster.Id),
            CreateBoard(DeviceType.R3lXp, "Slave", 12, 2, "DIS0110002",
                dictionaryId: dictR3lSlave.Id),

            // Spark (HMI con dizionario, motori/rostro senza)
            CreateBoard(DeviceType.Spark, "HMI", 20, 1, "DIS0060001",
                isPrimary: true, dictionaryId: dictSpark.Id),
            CreateBoard(DeviceType.Spark, "Motore DX", 21, 2, "DIS0060002"),
            CreateBoard(DeviceType.Spark, "Motore SX", 21, 3, "DIS0060003"),
            CreateBoard(DeviceType.Spark, "Rostro", 22, 4, "DIS0060004"),
        };
        context.Boards.AddRange(boards);
        await context.SaveChangesAsync();

        // === Variables per Standard ===
        var varDeviceStatus = CreateVariable(dictStandard.Id, "Device Status", 0x00, 0x10, "Bitmapped[2]",
            "Stato generale dispositivo", accessMode: AccessMode.ReadOnly, unit: null, minValue: null, maxValue: null);

        var standardVars = new[]
        {
            CreateVariable(dictStandard.Id, "Firmware Version", 0x00, 0x01, "UInt16",
                "Versione firmware (major.minor)", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictStandard.Id, "Firmware Build", 0x00, 0x02, "UInt16",
                "Build number firmware", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictStandard.Id, "Serial Number", 0x00, 0x03, "String[16]",
                "Numero seriale dispositivo", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictStandard.Id, "Production Date", 0x00, 0x04, "UInt32",
                "Data produzione (Unix timestamp)", accessMode: AccessMode.ReadOnly),
            varDeviceStatus,
            CreateVariable(dictStandard.Id, "Error Code", 0x00, 0x11, "UInt16",
                "Ultimo codice errore", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictStandard.Id, "Error Count", 0x00, 0x12, "UInt16",
                "Contatore errori totali", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictStandard.Id, "Uptime", 0x00, 0x20, "UInt32",
                "Tempo di attività", accessMode: AccessMode.ReadOnly, unit: "s"),
            CreateVariable(dictStandard.Id, "Boot Count", 0x00, 0x21, "UInt16",
                "Numero di riavvii", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictStandard.Id, "Debug Mode", 0x00, 0x30, "UInt8",
                "Modalità debug (0=off, 1=on)", accessMode: AccessMode.ReadWrite),
        };
        context.Variables.AddRange(standardVars);

        // === Variables per Optimus XP ===
        var varRelayStatus = CreateVariable(dictOptimusXp.Id, "Relay Status", 0x80, 0x20, "Bitmapped[1]",
            "Stato dei relè", accessMode: AccessMode.ReadOnly);

        var optimusVars = new[]
        {
            CreateVariable(dictOptimusXp.Id, "Temperature CPU", 0x80, 0x01, "Int16",
                "Temperatura CPU", accessMode: AccessMode.ReadOnly, unit: "°C/10", minValue: -400, maxValue: 1200),
            CreateVariable(dictOptimusXp.Id, "Temperature Board", 0x80, 0x02, "Int16",
                "Temperatura scheda", accessMode: AccessMode.ReadOnly, unit: "°C/10", minValue: -400, maxValue: 1000),
            CreateVariable(dictOptimusXp.Id, "Fan Speed", 0x80, 0x03, "UInt16",
                "Velocità ventola", accessMode: AccessMode.ReadOnly, unit: "RPM", minValue: 0, maxValue: 5000),
            CreateVariable(dictOptimusXp.Id, "Fan Target", 0x80, 0x04, "UInt16",
                "Velocità ventola target", accessMode: AccessMode.ReadWrite, unit: "RPM", minValue: 0, maxValue: 5000),
            CreateVariable(dictOptimusXp.Id, "Power Mode", 0x80, 0x10, "UInt8",
                "Modalità alimentazione (0=eco, 1=normal, 2=boost)", accessMode: AccessMode.ReadWrite, minValue: 0, maxValue: 2),
            CreateVariable(dictOptimusXp.Id, "Supply Voltage", 0x80, 0x11, "UInt16",
                "Tensione alimentazione", accessMode: AccessMode.ReadOnly, unit: "mV", minValue: 0, maxValue: 30000),
            varRelayStatus,
            CreateVariable(dictOptimusXp.Id, "Relay Control", 0x80, 0x21, "Bitmapped[1]",
                "Controllo relè", accessMode: AccessMode.ReadWrite),
        };
        context.Variables.AddRange(optimusVars);

        // === Variables per Eden XP ===
        var edenVars = new[]
        {
            CreateVariable(dictEdenXp.Id, "Lift Position", 0x80, 0x01, "Int32",
                "Posizione sollevatore", accessMode: AccessMode.ReadOnly, unit: "mm", minValue: 0, maxValue: 2000),
            CreateVariable(dictEdenXp.Id, "Lift Target", 0x80, 0x02, "Int32",
                "Posizione target sollevatore", accessMode: AccessMode.ReadWrite, unit: "mm", minValue: 0, maxValue: 2000),
            CreateVariable(dictEdenXp.Id, "Lift Speed", 0x80, 0x03, "UInt16",
                "Velocità sollevamento", accessMode: AccessMode.ReadWrite, unit: "mm/s", minValue: 1, maxValue: 100),
            CreateVariable(dictEdenXp.Id, "Weight", 0x80, 0x10, "UInt32",
                "Peso rilevato", accessMode: AccessMode.ReadOnly, unit: "g", minValue: 0, maxValue: 500000),
            CreateVariable(dictEdenXp.Id, "Weight Tare", 0x80, 0x11, "UInt32",
                "Tara peso", accessMode: AccessMode.ReadWrite, unit: "g", minValue: 0, maxValue: 50000),
            CreateVariable(dictEdenXp.Id, "Sensor Status", 0x80, 0x20, "Bitmapped[2]",
                "Stato sensori", accessMode: AccessMode.ReadOnly),
        };
        context.Variables.AddRange(edenVars);

        // === Variables per Pulsantiere ===
        var pulsantiereVars = new[]
        {
            CreateVariable(dictPulsantiere.Id, "Button State", 0x80, 0x01, "Bitmapped[2]",
                "Stato pulsanti (1 bit per pulsante)", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictPulsantiere.Id, "Button Event", 0x80, 0x02, "UInt8",
                "Ultimo evento pulsante", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictPulsantiere.Id, "LED State", 0x80, 0x10, "Bitmapped[2]",
                "Stato LED (1 bit per LED)", accessMode: AccessMode.ReadWrite),
            CreateVariable(dictPulsantiere.Id, "LED Blink", 0x80, 0x11, "Bitmapped[2]",
                "LED in lampeggio", accessMode: AccessMode.ReadWrite),
            CreateVariable(dictPulsantiere.Id, "Backlight Level", 0x80, 0x20, "UInt8",
                "Luminosità retroilluminazione", accessMode: AccessMode.ReadWrite, unit: "%", minValue: 0, maxValue: 100),
            CreateVariable(dictPulsantiere.Id, "Buzzer", 0x80, 0x21, "UInt8",
                "Controllo buzzer (0=off, 1-255=frequenza)", accessMode: AccessMode.ReadWrite),
        };
        context.Variables.AddRange(pulsantiereVars);

        // === Variables per Driver Motore ===
        var motoreVars = new[]
        {
            CreateVariable(dictMotore.Id, "Motor Speed", 0x80, 0x01, "Int16",
                "Velocità motore", accessMode: AccessMode.ReadOnly, unit: "RPM", minValue: -3000, maxValue: 3000),
            CreateVariable(dictMotore.Id, "Motor Target", 0x80, 0x02, "Int16",
                "Velocità target", accessMode: AccessMode.ReadWrite, unit: "RPM", minValue: -3000, maxValue: 3000),
            CreateVariable(dictMotore.Id, "Motor Current", 0x80, 0x03, "UInt16",
                "Corrente motore", accessMode: AccessMode.ReadOnly, unit: "mA", minValue: 0, maxValue: 20000),
            CreateVariable(dictMotore.Id, "Motor Temperature", 0x80, 0x04, "Int16",
                "Temperatura motore", accessMode: AccessMode.ReadOnly, unit: "°C/10", minValue: -200, maxValue: 1500),
            CreateVariable(dictMotore.Id, "Motor Status", 0x80, 0x10, "Bitmapped[1]",
                "Stato motore", accessMode: AccessMode.ReadOnly),
            CreateVariable(dictMotore.Id, "Motor Enable", 0x80, 0x11, "UInt8",
                "Abilitazione motore (0=off, 1=on)", accessMode: AccessMode.ReadWrite),
        };
        context.Variables.AddRange(motoreVars);

        await context.SaveChangesAsync();

        // === Bit Interpretations ===
        var bitInterpretations = new[]
        {
            // Device Status bits (Word 0)
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id,
                WordIndex = 0, BitIndex = 0, Meaning = "Power OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id,
                WordIndex = 0, BitIndex = 1, Meaning = "Communication OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id,
                WordIndex = 0, BitIndex = 2, Meaning = "Sensor OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id,
                WordIndex = 0, BitIndex = 7, Meaning = "Error Flag" },
            // Device Status bits (Word 1)
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id,
                WordIndex = 1, BitIndex = 0, Meaning = "Overtemp Warning" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id,
                WordIndex = 1, BitIndex = 1, Meaning = "Low Battery" },

            // Relay Status bits
            new BitInterpretationEntity { VariableId = varRelayStatus.Id,
                WordIndex = 0, BitIndex = 0, Meaning = "Relay 1 (Main Power)" },
            new BitInterpretationEntity { VariableId = varRelayStatus.Id,
                WordIndex = 0, BitIndex = 1, Meaning = "Relay 2 (Aux Power)" },
            new BitInterpretationEntity { VariableId = varRelayStatus.Id,
                WordIndex = 0, BitIndex = 2, Meaning = "Relay 3 (Heater)" },
            new BitInterpretationEntity { VariableId = varRelayStatus.Id,
                WordIndex = 0, BitIndex = 3, Meaning = "Relay 4 (Fan)" },
        };
        context.BitInterpretations.AddRange(bitInterpretations);

        // === Commands ===
        var cmdReadVar = new CommandEntity
        {
            Name = "Read Variable",
            CodeHigh = 0x01,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"address:UInt16\"]"
        };
        var cmdReadVarResp = new CommandEntity
        {
            Name = "Read Variable Response",
            CodeHigh = 0x01,
            CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"address:UInt16\",\"value:ByteArray\"]"
        };
        var cmdWriteVar = new CommandEntity
        {
            Name = "Write Variable",
            CodeHigh = 0x02,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"address:UInt16\",\"value:ByteArray\"]"
        };
        var cmdWriteVarResp = new CommandEntity
        {
            Name = "Write Variable Response",
            CodeHigh = 0x02,
            CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"address:UInt16\",\"result:UInt8\"]"
        };
        var cmdGetInfo = new CommandEntity
        {
            Name = "Get Device Info",
            CodeHigh = 0x10,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[]"
        };
        var cmdGetInfoResp = new CommandEntity
        {
            Name = "Get Device Info Response",
            CodeHigh = 0x10,
            CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"fwVersion:UInt16\",\"fwBuild:UInt16\",\"serial:String[16]\"]"
        };
        var cmdReset = new CommandEntity
        {
            Name = "Reset Device",
            CodeHigh = 0x20,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"mode:UInt8\"]"
        };
        var cmdResetResp = new CommandEntity
        {
            Name = "Reset Device Response",
            CodeHigh = 0x20,
            CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"result:UInt8\"]"
        };
        var cmdSetConfig = new CommandEntity
        {
            Name = "Set Configuration",
            CodeHigh = 0x30,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"configId:UInt8\",\"value:UInt32\"]"
        };
        var cmdGetConfig = new CommandEntity
        {
            Name = "Get Configuration",
            CodeHigh = 0x31,
            CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"configId:UInt8\"]"
        };
        var cmdGetConfigResp = new CommandEntity
        {
            Name = "Get Configuration Response",
            CodeHigh = 0x31,
            CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"configId:UInt8\",\"value:UInt32\"]"
        };

        var commands = new[] { cmdReadVar, cmdReadVarResp, cmdWriteVar, cmdWriteVarResp,
            cmdGetInfo, cmdGetInfoResp, cmdReset, cmdResetResp, cmdSetConfig, cmdGetConfig, cmdGetConfigResp };
        context.Commands.AddRange(commands);
        await context.SaveChangesAsync();

        // === Command Device States ===
        // Disabilita alcuni comandi per dispositivi specifici
        var commandDeviceStates = new[]
        {
            // Reset non disponibile su SherpaSlim
            new CommandDeviceStateEntity { CommandId = cmdReset.Id, DeviceType = DeviceType.SherpaSlim, IsEnabled = false },

            // Set Config non disponibile su Pulsantiere (troppo semplici)
            new CommandDeviceStateEntity { CommandId = cmdSetConfig.Id, DeviceType = DeviceType.Gradino, IsEnabled = false },
        };
        context.CommandDeviceStates.AddRange(commandDeviceStates);

        // === Variable Device States ===
        // Override per-device su variabili Standard (BR-009)
        // "Debug Mode" non supportata su SherpaSlim e Gradino
        var varDebugMode = standardVars.First(v => v.Name == "Debug Mode");
        var variableDeviceStates = new[]
        {
            new VariableDeviceStateEntity
            {
                VariableId = varDebugMode.Id,
                DeviceType = DeviceType.SherpaSlim,
                IsEnabled = false
            },
            new VariableDeviceStateEntity
            {
                VariableId = varDebugMode.Id,
                DeviceType = DeviceType.Gradino,
                IsEnabled = false
            },
        };
        context.VariableDeviceStates.AddRange(variableDeviceStates);

        await context.SaveChangesAsync();
    }

    private static BoardEntity CreateBoard(DeviceType deviceType,
        string name, int firmwareType, int boardNumber, string? partNumber,
        bool isPrimary = false, int? dictionaryId = null)
    {
        var protocolAddress = ((uint)deviceType << 16) |
            (((uint)firmwareType & 0x03FF) << 6) |
            ((uint)boardNumber & 0x003F);

        return new BoardEntity
        {
            DeviceType = deviceType,
            Name = name,
            FirmwareType = firmwareType,
            BoardNumber = boardNumber,
            PartNumber = partNumber,
            ProtocolAddress = protocolAddress,
            IsPrimary = isPrimary,
            DictionaryId = dictionaryId
        };
    }

    private static VariableEntity CreateVariable(
        int dictionaryId,
        string name,
        byte addressHigh,
        byte addressLow,
        string dataType,
        string? description = null,
        AccessMode accessMode = AccessMode.ReadWrite,
        string? unit = null,
        float? minValue = null,
        float? maxValue = null)
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
            AccessMode = accessMode,
            IsEnabled = true,
            Unit = unit,
            MinValue = minValue,
            MaxValue = maxValue
        };
    }

    private static DataTypeKind ParseDataTypeKind(string dataType)
    {
        if (dataType.StartsWith("String")) return DataTypeKind.String;
        if (dataType.StartsWith("Array")) return DataTypeKind.Array;
        if (dataType.StartsWith("Bitmapped")) return DataTypeKind.Bitmapped;

        return dataType switch
        {
            "UInt8" => DataTypeKind.UInt8,
            "Int8" => DataTypeKind.Int8,
            "UInt16" => DataTypeKind.UInt16,
            "Int16" => DataTypeKind.Int16,
            "UInt32" => DataTypeKind.UInt32,
            "Int32" => DataTypeKind.Int32,
            "Float" => DataTypeKind.Float,
            "Bool" => DataTypeKind.Bool,
            _ => DataTypeKind.Other
        };
    }

    private static int? ParseDataTypeParam(string dataType)
    {
        // Estrae il parametro da tipi come "String[16]" o "Bitmapped[2]"
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
