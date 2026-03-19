using Core.Enums;
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
            new UserEntity { Username = "michele.pignedoli", DisplayName = "Michele Pignedoli" },
            new UserEntity { Username = "lorenzo.vecchi", DisplayName = "Lorenzo Vecchi" },
            new UserEntity { Username = "andrea.acunzo", DisplayName = "Andrea Acunzo" },
            new UserEntity { Username = "alessandro.goldoni", DisplayName = "Alessandro Goldoni" },
            new UserEntity { Username = "luca.veronelli", DisplayName = "Luca Veronelli" }
        };
        context.Users.AddRange(users);

        // === BoardTypes ===
        var btMadreOptimus = new BoardTypeEntity { Name = "Madre Optimus", FirmwareType = 17 };
        var btMadreEden = new BoardTypeEntity { Name = "Madre Eden", FirmwareType = 18 };
        var btPulsantiera4 = new BoardTypeEntity { Name = "Pulsantiera 4x4", FirmwareType = 4 };
        var btPulsantiera8 = new BoardTypeEntity { Name = "Pulsantiera 8x8", FirmwareType = 8 };
        var btSherpa = new BoardTypeEntity { Name = "Sherpa Slim", FirmwareType = 20 };
        var btDisplay = new BoardTypeEntity { Name = "Display LCD", FirmwareType = 10 };
        var btMotore = new BoardTypeEntity { Name = "Driver Motore", FirmwareType = 25 };

        var boardTypes = new[] { btMadreOptimus, btMadreEden, btPulsantiera4, btPulsantiera8, btSherpa, btDisplay, btMotore };
        context.BoardTypes.AddRange(boardTypes);
        await context.SaveChangesAsync();

        // === Boards ===
        var boards = new[]
        {
            // Optimus
            CreateBoard(DeviceType.Optimus, btMadreOptimus.Id, "Madre Optimus #1", 1, "DIS0020001"),
            CreateBoard(DeviceType.Optimus, btPulsantiera4.Id, "Tastiera Optimus", 1, "DIS0020010"),
            CreateBoard(DeviceType.Optimus, btDisplay.Id, "Display Optimus", 1, "DIS0020020"),

            // Eden
            CreateBoard(DeviceType.Eden, btMadreEden.Id, "Madre Eden #1", 1, "DIS0030001"),
            CreateBoard(DeviceType.Eden, btPulsantiera8.Id, "Tastiera Eden Main", 1, "DIS0030010"),
            CreateBoard(DeviceType.Eden, btPulsantiera8.Id, "Tastiera Eden Aux", 2, "DIS0030011"),
            CreateBoard(DeviceType.Eden, btMotore.Id, "Driver Motore Eden", 1, "DIS0030030"),

            // OptimusXp
            CreateBoard(DeviceType.OptimusXp, btMadreOptimus.Id, "Madre OptimusXP Master", 1, "DIS0100001"),
            CreateBoard(DeviceType.OptimusXp, btMadreOptimus.Id, "Madre OptimusXP Slave", 2, "DIS0100002"),
            CreateBoard(DeviceType.OptimusXp, btPulsantiera4.Id, "Tastiera XP 1", 1, "DIS0100010"),
            CreateBoard(DeviceType.OptimusXp, btPulsantiera4.Id, "Tastiera XP 2", 2, "DIS0100011"),
            CreateBoard(DeviceType.OptimusXp, btPulsantiera4.Id, "Tastiera XP 3", 3, "DIS0100012"),

            // SherpaSlim
            CreateBoard(DeviceType.SherpaSlim, btSherpa.Id, "Sherpa Slim Main", 1, "DIS0010001"),
        };
        context.Boards.AddRange(boards);
        await context.SaveChangesAsync();

        // === Dictionaries ===
        var dictStandard = new DictionaryEntity
        {
            Name = "Standard",
            Description = "Variabili comuni a tutti i dispositivi STEM",
            BoardTypeId = null  // Standard = senza BoardType
        };

        var dictOptimus = new DictionaryEntity
        {
            Name = "Optimus",
            Description = "Variabili specifiche per schede madre Optimus (FW Type 17)",
            BoardTypeId = btMadreOptimus.Id
        };

        var dictEden = new DictionaryEntity
        {
            Name = "Eden",
            Description = "Variabili specifiche per schede madre Eden (FW Type 18)",
            BoardTypeId = btMadreEden.Id
        };

        var dictPulsantiere = new DictionaryEntity
        {
            Name = "Pulsantiere",
            Description = "Variabili per tastiere e pulsantiere 4x4 e 8x8",
            BoardTypeId = btPulsantiera4.Id
        };

        var dictMotore = new DictionaryEntity
        {
            Name = "Driver Motore",
            Description = "Variabili per controllo motori",
            BoardTypeId = btMotore.Id
        };

        context.Dictionaries.AddRange(dictStandard, dictOptimus, dictEden, dictPulsantiere, dictMotore);
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

        // === Variables per Optimus ===
        var varRelayStatus = CreateVariable(dictOptimus.Id, "Relay Status", 0x80, 0x20, "Bitmapped[1]", 
            "Stato dei relè", accessMode: AccessMode.ReadOnly);

        var optimusVars = new[]
        {
            CreateVariable(dictOptimus.Id, "Temperature CPU", 0x80, 0x01, "Int16", 
                "Temperatura CPU", accessMode: AccessMode.ReadOnly, unit: "°C/10", minValue: -400, maxValue: 1200),
            CreateVariable(dictOptimus.Id, "Temperature Board", 0x80, 0x02, "Int16", 
                "Temperatura scheda", accessMode: AccessMode.ReadOnly, unit: "°C/10", minValue: -400, maxValue: 1000),
            CreateVariable(dictOptimus.Id, "Fan Speed", 0x80, 0x03, "UInt16", 
                "Velocità ventola", accessMode: AccessMode.ReadOnly, unit: "RPM", minValue: 0, maxValue: 5000),
            CreateVariable(dictOptimus.Id, "Fan Target", 0x80, 0x04, "UInt16", 
                "Velocità ventola target", accessMode: AccessMode.ReadWrite, unit: "RPM", minValue: 0, maxValue: 5000),
            CreateVariable(dictOptimus.Id, "Power Mode", 0x80, 0x10, "UInt8", 
                "Modalità alimentazione (0=eco, 1=normal, 2=boost)", accessMode: AccessMode.ReadWrite, minValue: 0, maxValue: 2),
            CreateVariable(dictOptimus.Id, "Supply Voltage", 0x80, 0x11, "UInt16", 
                "Tensione alimentazione", accessMode: AccessMode.ReadOnly, unit: "mV", minValue: 0, maxValue: 30000),
            varRelayStatus,
            CreateVariable(dictOptimus.Id, "Relay Control", 0x80, 0x21, "Bitmapped[1]", 
                "Controllo relè", accessMode: AccessMode.ReadWrite),
        };
        context.Variables.AddRange(optimusVars);

        // === Variables per Eden ===
        var edenVars = new[]
        {
            CreateVariable(dictEden.Id, "Lift Position", 0x80, 0x01, "Int32", 
                "Posizione sollevatore", accessMode: AccessMode.ReadOnly, unit: "mm", minValue: 0, maxValue: 2000),
            CreateVariable(dictEden.Id, "Lift Target", 0x80, 0x02, "Int32", 
                "Posizione target sollevatore", accessMode: AccessMode.ReadWrite, unit: "mm", minValue: 0, maxValue: 2000),
            CreateVariable(dictEden.Id, "Lift Speed", 0x80, 0x03, "UInt16", 
                "Velocità sollevamento", accessMode: AccessMode.ReadWrite, unit: "mm/s", minValue: 1, maxValue: 100),
            CreateVariable(dictEden.Id, "Weight", 0x80, 0x10, "UInt32", 
                "Peso rilevato", accessMode: AccessMode.ReadOnly, unit: "g", minValue: 0, maxValue: 500000),
            CreateVariable(dictEden.Id, "Weight Tare", 0x80, 0x11, "UInt32", 
                "Tara peso", accessMode: AccessMode.ReadWrite, unit: "g", minValue: 0, maxValue: 50000),
            CreateVariable(dictEden.Id, "Sensor Status", 0x80, 0x20, "Bitmapped[2]", 
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
            // Device Status bits
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 0, Meaning = "Power OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 1, Meaning = "Communication OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 2, Meaning = "Sensor OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 7, Meaning = "Error Flag" },

            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Eden, 
                WordIndex = 0, BitIndex = 0, Meaning = "Power OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Eden, 
                WordIndex = 0, BitIndex = 1, Meaning = "Lift OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Eden, 
                WordIndex = 0, BitIndex = 2, Meaning = "Weight OK" },
            new BitInterpretationEntity { VariableId = varDeviceStatus.Id, DeviceType = DeviceType.Eden, 
                WordIndex = 0, BitIndex = 7, Meaning = "Error Flag" },

            // Relay Status bits (Optimus)
            new BitInterpretationEntity { VariableId = varRelayStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 0, Meaning = "Relay 1 (Main Power)" },
            new BitInterpretationEntity { VariableId = varRelayStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 1, Meaning = "Relay 2 (Aux Power)" },
            new BitInterpretationEntity { VariableId = varRelayStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 2, Meaning = "Relay 3 (Heater)" },
            new BitInterpretationEntity { VariableId = varRelayStatus.Id, DeviceType = DeviceType.Optimus, 
                WordIndex = 0, BitIndex = 3, Meaning = "Relay 4 (Fan)" },
        };
        context.BitInterpretations.AddRange(bitInterpretations);

        // === Commands ===
        var cmdReadVar = new CommandEntity
        {
            Name = "Read Variable",
            CodeHigh = 0x01, CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"address:UInt16\"]"
        };
        var cmdReadVarResp = new CommandEntity
        {
            Name = "Read Variable Response",
            CodeHigh = 0x01, CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"address:UInt16\",\"value:ByteArray\"]"
        };
        var cmdWriteVar = new CommandEntity
        {
            Name = "Write Variable",
            CodeHigh = 0x02, CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"address:UInt16\",\"value:ByteArray\"]"
        };
        var cmdWriteVarResp = new CommandEntity
        {
            Name = "Write Variable Response",
            CodeHigh = 0x02, CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"address:UInt16\",\"result:UInt8\"]"
        };
        var cmdGetInfo = new CommandEntity
        {
            Name = "Get Device Info",
            CodeHigh = 0x10, CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[]"
        };
        var cmdGetInfoResp = new CommandEntity
        {
            Name = "Get Device Info Response",
            CodeHigh = 0x10, CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"fwVersion:UInt16\",\"fwBuild:UInt16\",\"serial:String[16]\"]"
        };
        var cmdReset = new CommandEntity
        {
            Name = "Reset Device",
            CodeHigh = 0x20, CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"mode:UInt8\"]"
        };
        var cmdResetResp = new CommandEntity
        {
            Name = "Reset Device Response",
            CodeHigh = 0x20, CodeLow = 0x00,
            IsResponse = true,
            ParametersJson = "[\"result:UInt8\"]"
        };
        var cmdSetConfig = new CommandEntity
        {
            Name = "Set Configuration",
            CodeHigh = 0x30, CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"configId:UInt8\",\"value:UInt32\"]"
        };
        var cmdGetConfig = new CommandEntity
        {
            Name = "Get Configuration",
            CodeHigh = 0x31, CodeLow = 0x00,
            IsResponse = false,
            ParametersJson = "[\"configId:UInt8\"]"
        };
        var cmdGetConfigResp = new CommandEntity
        {
            Name = "Get Configuration Response",
            CodeHigh = 0x31, CodeLow = 0x00,
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

        await context.SaveChangesAsync();
    }

    private static BoardEntity CreateBoard(DeviceType deviceType, int boardTypeId, string name, int boardNumber, string? partNumber)
    {
        // Calcola l'indirizzo protocol
        var protocolAddress = ((uint)deviceType << 16) | (((uint)boardTypeId & 0x03FF) << 6) | ((uint)boardNumber & 0x003F);

        return new BoardEntity
        {
            DeviceType = deviceType,
            BoardTypeId = boardTypeId,
            Name = name,
            BoardNumber = boardNumber,
            PartNumber = partNumber,
            ProtocolAddress = protocolAddress
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
