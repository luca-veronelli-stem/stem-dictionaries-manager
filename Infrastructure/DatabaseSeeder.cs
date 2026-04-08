using Core.Enums;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Popola il database con dati iniziali.
/// Utenti + Dispositivi + Comandi + Schede: il resto viene inserito dalla GUI.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Se esistono già dati, non fare nulla
        if (await context.Users.AnyAsync())
            return;
        if (await context.Devices.AnyAsync())
            return;
        if (await context.Commands.AnyAsync())
            return;
        if (await context.Boards.AnyAsync())
            return;

        // === Utenti del team firmware STEM ===
        var users = new[]
        {
            new UserEntity { Username = "luca.veronelli", DisplayName = "Luca Veronelli" },
            new UserEntity { Username = "alessandro.goldoni", DisplayName = "Alessandro Goldoni" },
            new UserEntity { Username = "andrea.acunzo", DisplayName = "Andrea Acunzo" },
            new UserEntity { Username = "michele.pignedoli", DisplayName = "Michele Pignedoli" },
            new UserEntity { Username = "lorenzo.vecchi", DisplayName = "Lorenzo Vecchi" }
        };
        context.Users.AddRange(users);

        // === Dispositivi STEM ===
        // MachineCode 6 è riservato per BLE Module (BR-015)
        var devices = new[]
        {
            new DeviceEntity { Name = "Sherpa Slim", MachineCode = 1, Description = "Sistema di caricamento assistito a controllo elettronico" },
            new DeviceEntity { Name = "TopLift-M", MachineCode = 2, Description = "Sollevatori oleodinamici serie civile" },
            new DeviceEntity { Name = "Eden-XP", MachineCode = 3, Description = "Supporto barella oleodinamico con altezza e inclinazione regolabili, e molleggio" },
            new DeviceEntity { Name = "Gradino", MachineCode = 4, Description = "Gradini automatici" },
            new DeviceEntity { Name = "Spyke", MachineCode = 5, Description = "Barella" },
            new DeviceEntity { Name = "Spark", MachineCode = 7, Description = "Barella elettrica robotizzata" },
            new DeviceEntity { Name = "TopLift-A2", MachineCode = 8, Description = "Sollevatori oleodinamici serie militare" },
            new DeviceEntity { Name = "O3Z-Tech", MachineCode = 9, Description = "Sistema di sanificazione per ambienti" },
            new DeviceEntity { Name = "Optimus-XP", MachineCode = 10, Description = "Supporto barelle elettriche oleodinamico con altezza regolabile, e molleggio" },
            new DeviceEntity { Name = "R3L-XP", MachineCode = 11, Description = "Supporto barella elettromeccanico con altezza e inclinazione regolabili" },
            new DeviceEntity { Name = "Eden-BS8", MachineCode = 12, Description = "Supporto barelle elettriche oleodinamico con altezza ed inclinazione regolabili, e molleggio" },
        };
        context.Devices.AddRange(devices);

        // === Comandi protocollo STEM (da comandi.csv) ===
        // Regola: CodeHigh = 0x00 per comandi, 0x80 per risposte
        var commands = new[]
        {
            // 0x00 - Versione protocollo
            Cmd("Versione protocollo", 0x00, 0x00, false),
            Cmd("Versione protocollo risposta", 0x80, 0x00, true, "2|Versione"),

            // 0x01 - Leggi variabile logica
            Cmd("Leggi variabile logica", 0x00, 0x01, false, "2|Indirizzo"),
            Cmd("Leggi variabile logica risposta", 0x80, 0x01, true,
                "2|Indirizzo", "N|Valori (Come da dizionario)"),

            // 0x02 - Scrivi variabile logica
            Cmd("Scrivi variabile logica", 0x00, 0x02, false,
                "2|Indirizzo", "N|Valori (Come da dizionario)"),
            Cmd("Scrivi variabile logica risposta", 0x80, 0x02, true,
                "2|Indirizzo"),

            // 0x03 - Leggi area di memoria
            Cmd("Leggi area di memoria", 0x00, 0x03, false,
                "2|Indirizzo iniziale", "2|Numero variabili"),
            Cmd("Leggi area di memoria risposta", 0x80, 0x03, true,
                "2|Indirizzo iniziale", "2|Numero variabili", "N|Valori (Come da numero variabili)"),

            // 0x04 - Scrivi area di memoria
            Cmd("Scrivi area di memoria", 0x00, 0x04, false,
                "2|Indirizzo", "2|Dimensione array", "N|Valori (Come da dimensione array)"),
            Cmd("Scrivi area di memoria risposta", 0x80, 0x04, true,
                "2|Indirizzo", "2|Dimensione array"),

            // 0x05 - Avvia bootloader
            Cmd("Avvia bootloader", 0x00, 0x05, false),
            Cmd("Avvia bootloader risposta", 0x80, 0x05, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x06 - Arresta bootloader
            Cmd("Arresta bootloader", 0x00, 0x06, false),
            Cmd("Arresta bootloader risposta", 0x80, 0x06, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x07 - Aggiorna pagina bootloader
            Cmd("Aggiorna pagina bootloader", 0x00, 0x07, false,
                "2|Firmware type", "4|Numero pagina", "4|Dimensione pagina", "4|Reserved", 
                "N|Valori (Come da dimensione pagina)"),
            Cmd("Aggiorna pagina bootloader risposta", 0x80, 0x07, true,
                "2|Firmware type", "4|Numero pagina", "4|Dimensione pagina", "4|Reserved", "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x08 - Avvia dispositivo
            Cmd("Avvia dispositivo", 0x00, 0x08, false),
            Cmd("Avvia dispositivo risposta", 0x80, 0x08, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x09 - Arresta dispositivo
            Cmd("Arresta dispositivo", 0x00, 0x09, false),
            Cmd("Arresta dispositivo risposta", 0x80, 0x09, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x0A - Riavvia dispositivo
            Cmd("Riavvia dispositivo", 0x00, 0x0A, false),
            Cmd("Riavvia dispositivo risposta", 0x80, 0x0A, true),

            // 0x0B - Unlock caratteristica BLE
            Cmd("Unlock caratteristica BLE", 0x00, 0x0B, false),

            // 0x0C - Lock caratteristica BLE
            Cmd("Lock caratteristica BLE", 0x00, 0x0C, false),

            // 0x0D - Su da pulsantiera
            Cmd("Su da pulsantiera", 0x00, 0x0D, false, "1|Stato (0 = rilasciato, 1 = premuto)"),

            // 0x0E - Giù da pulsantiera
            Cmd("Giù da pulsantiera", 0x00, 0x0E, false, "1|Stato (0 = rilasciato, 1 = premuto)"),

            // 0x0F - Inizia auto apprendimento
            Cmd("Inizia auto apprendimento", 0x00, 0x0F, false),
            Cmd("Inizia auto apprendimento risposta", 0x80, 0x0F, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x10 - Salva posizione
            Cmd("Salva posizione", 0x00, 0x10, false, "1|Direzione (1 = scarico, 2 = carico, 3 = entrambe)", "1|Stato movimento (0 = ferma, 1 = in movimento)"),
            Cmd("Salva posizione risposta", 0x80, 0x10, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x11 - Finisci auto apprendimento
            Cmd("Finisci auto apprendimento", 0x00, 0x11, false),
            Cmd("Finisci auto apprendimento risposta", 0x80, 0x11, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x12 - Muovi piano
            Cmd("Muovi piano", 0x00, 0x12, false, "1|Muovi (0 = no, 1 = si)", "4|Angolo"),
            Cmd("Muovi piano risposta", 0x80, 0x12, true, "1|Risultato (0 = ok, !0 = non puo muoversi)"),

            // 0x13 - Abilita pairing BLE
            Cmd("Abilita pairing BLE", 0x00, 0x13, false, "10|Nome dispositivo"),

            // 0x14 - Disabilita pairing BLE
            Cmd("Disabilita pairing BLE", 0x00, 0x14, false),

            // 0x15 - Configura telemetria
            Cmd("Configura telemetria", 0x00, 0x15, false, "4|Tipo di telemetria (codice univoco deciso dall'amministratore)",
                "4|Indirizzo di destinazione (indirizzo di protocollo STEM a cui inviare la telemetria)", 
                "1|Istanza telemetria (numero del task che ospita la telemetria)", "2|Periodo (ms)",
                "4|Indirizzo scheda (indirizzo del protocollo STEM da cui rilevare la telemetria)", 
                "N|Indirizzi logici (2 bytes ciascuno al massimo SP_TEL_N_VARS, variabile definita nel firmware)"),
            Cmd("Configura telemetria risposta", 0x80, 0x15, true),

            // 0x16 - Avvia telemetria
            Cmd("Avvia telemetria", 0x00, 0x16, false, "1|Istanza telemetria"),
            Cmd("Avvia telemetria risposta", 0x80, 0x16, true),

            // 0x17 - Arresta telemetria
            Cmd("Arresta telemetria", 0x00, 0x17, false, "1|Istanza telemetria"),
            Cmd("Arresta telemetria risposta", 0x80, 0x17, true),

            // 0x18 - Pacchetto di telemetria
            Cmd("Pacchetto di telemetria", 0x00, 0x18, false,"4|Tipo di telemetria", "N|Dati"),

            // 0x19 - Configura log
            Cmd("Configura log", 0x00, 0x19, false, "4|Tipo di log (codice univoco deciso dall'amministratore)", 
                "1|Istanza log", "4|Indirizzo scheda evento", "2|Indirizzo logico evento",
                "4|Soglia evento", "1|Tipo trigger", "1|Direzione trigger", "2|Periodo (ms)",
                "4|Indirizzo scheda", "N|Indirizzi logici (2 bytes ciascuno al massimo SP_TEL_N_VARS, variabile definita nel firmware)"),

            // 0x1A - Avvia log
            Cmd("Avvia log", 0x00, 0x1A, false, "1|Istanza log"),
            Cmd("Avvia log risposta", 0x80, 0x1A, true),

            // 0x1B - Arresta log
            Cmd("Arresta log", 0x00, 0x1B, false, "1|Istanza log"),
            Cmd("Arresta log risposta", 0x80, 0x1B, true),

            // 0x1C - Richiesta log
            Cmd("Richiesta log", 0x00, 0x1C, false, "4|Index (univoco incrementale assegnato dal dispositivo ai records)"),
            Cmd("Richiesta log risposta", 0x80, 0x1C, true,
                "4|Index", "4|Tipo di log", "4|Timestamp", "N|Dati"),

            // 0x1D - Stato connessione client BLE
            Cmd("Stato connessione BLE", 0x00, 0x1D, false),
            Cmd("Stato connessione BLE risposta", 0x80, 0x1D, true, "1|Stato (0 = non connesso, !0 = connesso)", "1|RSSI connessione"),

            // 0x1E - Connessione BLE
            Cmd("Imposta connessione BLE", 0x00, 0x1E, false),

            // 0x1F - Richiesta configurazione attuale log
            Cmd("Richiesta configurazione attuale log", 0x00, 0x1F, false, "1|Istanza log"),
            Cmd("Richiesta configurazione attuale log risposta", 0x80, 0x1F, true,
                "4|Tipo di log", "1|Istanza log","4|Indirizzo scheda evento","2|Indirizzo logico evento",
                "4|Soglia evento","1|Tipo trigger", "1|Direzione trigger", "2|Periodo (ms)",
                "4|Indirizzo scheda","N|Indirizzi logici (2 bytes ciascuno al massimo SP_TEL_N_VARS, variabile definita nel firmware)"),

            // 0x20 - Richiedi info fat log
            Cmd("Richiedi fat log", 0x00, 0x20, false, "1|Istanza log"),
            Cmd("Richiedi fat log risposta", 0x80, 0x20, true, "4|Usati", "4|Primo", "4|Ultimo"),

            // 0x21 - Inizia sessione gateway
            Cmd("Avvia sessione gateway", 0x00, 0x21, false),

            // 0x22 - Arresta sessione gateway
            Cmd("Arresta sessione gateway", 0x00, 0x22, false),

            // 0x23 - Indirizzamento automatico chi siete
            Cmd("Indirizzamento automatico chi siete", 0x00, 0x23, false,
                "1|Machine type", "2|Firmware type", "1|Reset battezzamento (0 = no, 1 = si)"),

            // 0x24 - Indirizzamento automatico chi sono
            Cmd("Indirizzamento automatico chi sono", 0x00, 0x24, false,
                "1|Machine type", "2|Firmware type","12|MAC address"),

            // 0x25 - Indirizzamento automatico battezzati
            Cmd("Indirizzamento automatico battezzati", 0x00, 0x25, false, "12|MAC address", "4|Indirizzo protocollo STEM"),

            // 0x26 - Indirizzamento automatico risultato battezzamento
            Cmd("Indirizzamento automatico risultato battezzamento", 0x00, 0x26, false, "12|MAC address", "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x27 - Avvia indirizzamento automatico
            Cmd("Avvia indirizzamento automatico", 0x00, 0x27, false, "2|Firmware type"),

            // 0x28 - Up/Down da telecomando
            Cmd("Up/Down da telecomando", 0x00, 0x28, false, "1|Stato (0 = rilasciato, !0 = premuto)"),

            // 0x29 - Reset valori EEPROM al default
            Cmd("Reset valori EEPROM al default", 0x00, 0x29, false),
            Cmd("Reset valori EEPROM al default risposta", 0x80, 0x29, true, "1|Risultato (0 = ok, !0 = codice errore)"),

            // 0x2A - Esegui autotest
            Cmd("Esegui autotest", 0x00, 0x2A, false),
            Cmd("Esegui autotest risposta", 0x80, 0x2A, true, "1|Risultato (0 = ok, !0 = codice errore)"),
        };
        context.Commands.AddRange(commands);

        // Salva prima di inserire le schede, perché hanno FK verso i dispositivi
        await context.SaveChangesAsync();

        // === Schede STEM (da indirizzi.csv) ===
        // ProtocolAddress: (MACHINE << 16) | ((FW & 0x3FF) << 6) | (BOARD_NUMBER & 0x3F)
        // DictionaryId: assegnato dopo il seed dei dizionari
        // BLE Module (MC=6): skippato (BR-015)
        var boards = new[]
        {
            // Sherpa Slim (MC=1)
            Brd(1, devices[0].Id, "Azionamento",    1, 1, true),
            Brd(1, devices[0].Id, "Pulsantiera",    2, 1, false),

            // TopLift-M (MC=2)
            Brd(2, devices[1].Id, "Madre",           3, 1, true),
            Brd(2, devices[1].Id, "Pulsantiera",     4, 1, false),

            // Eden-XP (MC=3)
            Brd(3, devices[2].Id, "Madre",           5, 1, true),
            Brd(3, devices[2].Id, "Pulsantiera 1",   4, 1, false),
            Brd(3, devices[2].Id, "Pulsantiera 2",   4, 2, false),
            Brd(3, devices[2].Id, "Pulsantiera 3",   4, 3, false),

            // Gradino (MC=4)
            Brd(4, devices[3].Id, "Azionamento",     6, 1, true),

            // Spyke (MC=5)
            Brd(5, devices[4].Id, "Display",         8, 1, true),
            Brd(5, devices[4].Id, "Gateway",         7, 1, false),
            Brd(5, devices[4].Id, "HMI",             9, 1, false),

            // Spark (MC=7)
            Brd(7, devices[5].Id, "HMI",            11, 1, true),
            Brd(7, devices[5].Id, "Motori DX",      12, 1, false),
            Brd(7, devices[5].Id, "Motori SX",      12, 2, false),
            Brd(7, devices[5].Id, "Rostro",         13, 1, false),

            // TopLift-A2 (MC=8)
            Brd(8, devices[6].Id, "Madre",           14, 1, true),
            Brd(8, devices[6].Id, "Pulsantiera 1",   15, 1, false),
            Brd(8, devices[6].Id, "Pulsantiera 2",   15, 2, false),
            Brd(8, devices[6].Id, "Pulsantiera 3",   15, 3, false),
            Brd(8, devices[6].Id, "Pulsantiera vecchia", 4, 1, false),

            // O3Z-Tech (MC=9)
            Brd(9, devices[7].Id, "Display",         16, 1, true),

            // Optimus-XP (MC=10)
            Brd(10, devices[8].Id, "Madre",          17, 1, true),
            Brd(10, devices[8].Id, "Pulsantiera 1",   4, 1, false),
            Brd(10, devices[8].Id, "Pulsantiera 2",   4, 2, false),
            Brd(10, devices[8].Id, "Pulsantiera 3",   4, 3, false),

            // R3L-XP (MC=11)
            Brd(11, devices[9].Id, "Madre Master",   18, 1, true),
            Brd(11, devices[9].Id, "Madre Slave",    20, 1, false),
            Brd(11, devices[9].Id, "Pulsantiera 1",   4, 1, false),
            Brd(11, devices[9].Id, "Pulsantiera 2",   4, 2, false),
            Brd(11, devices[9].Id, "Pulsantiera 3",   4, 3, false),

            // Eden-BS8 (MC=12)
            Brd(12, devices[10].Id, "Madre",         19, 1, true),
            Brd(12, devices[10].Id, "Pulsantiera 1",  4, 1, false),
            Brd(12, devices[10].Id, "Pulsantiera 2",  4, 2, false),
            Brd(12, devices[10].Id, "Pulsantiera 3",  4, 3, false),
        };
        context.Boards.AddRange(boards);

        await context.SaveChangesAsync();

        // === Dizionario Standard (da dati_standard.CSV) ===
        var standardVariables = await SeedStandardDictionaryAsync(context);

        // === Dizionario Pulsantiere (da pulsantiere.CSV) ===
        var pulsantiereDictionary = await SeedPulsantiereDictionaryAsync(context, boards);

        // === Override variabili standard per-dizionario ===
        await SeedPulsantiereStandardOverridesAsync(
            context, pulsantiereDictionary, standardVariables);

        // === Dizionario Display Spyke (da hmi_spyke.CSV) ===
        var displaySpykeDictionary = await SeedDisplaySpykeDictionaryAsync(
            context, boards, devices[4]);

        // === Override variabili standard per Display Spyke ===
        await SeedDisplaySpykeOverridesAsync(
            context, displaySpykeDictionary, standardVariables);

        // === Dizionario Gateway Spyke (da gateway_spyke.CSV) ===
        var gatewaySpykeDictionary = await SeedGatewaySpykeDictionaryAsync(
            context, boards, devices[4]);

        // === Override variabili standard per Gateway Spyke ===
        await SeedGatewaySpykeOverridesAsync(
            context, gatewaySpykeDictionary, standardVariables);

        // === Dizionario Gradino (da gradino.CSV) ===
        var gradinoDictionary = await SeedGradinoDictionaryAsync(
            context, boards, devices[3]);

        // === Override variabili standard per Gradino ===
        await SeedGradinoOverridesAsync(
            context, gradinoDictionary, standardVariables);

        // === Dizionario Eden-XP (da eden-xp.CSV) ===
        var edenXPDictionary = await SeedEdenXPDictionaryAsync(
            context, boards, devices[2]);

        // === Override variabili standard per Eden-XP ===
        await SeedEdenXPOverridesAsync(
            context, edenXPDictionary, standardVariables);
    }

    /// <summary>
    /// Crea il dizionario standard con le variabili comuni a tutti i dispositivi STEM.
    /// Fonte: Docs/Dictionaries/dati_standard.CSV
    /// AddressHigh = 0x00 per tutte le variabili standard.
    /// BitInterpretations: vuote nello standard, i device le overridano.
    /// </summary>
    private static async Task<VariableEntity[]> SeedStandardDictionaryAsync(AppDbContext context)
    {
        var dictionary = new DictionaryEntity
        {
            Name = "Standard",
            Description = "Dizionario variabili standard comune a tutti i dispositivi STEM",
            IsStandard = true
        };
        context.Dictionaries.Add(dictionary);
        await context.SaveChangesAsync();

        var variables = new[]
        {
            // 0x0000 — Firmware macchina
            Var(dictionary.Id, "Firmware macchina", 0x00,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, format: "255.255", min: 0, max: 255.255,
                description: "Versione firmware globale della macchina"),

            // 0x0001 — Firmware scheda
            Var(dictionary.Id, "Firmware scheda", 0x01,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, format: "255.255", min: 0, max: 255.255,
                description: "Versione firmware della singola scheda"),

            // 0x0002 — Modello
            Var(dictionary.Id, "Modello", 0x02,
                DataTypeKind.String, "String[20]",
                AccessMode.ReadOnly, dataTypeParam: 20,
                description: "Nome modello del dispositivo"),

            // 0x0003 — Matricola
            Var(dictionary.Id, "Matricola", 0x03,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967295,
                description: "Numero di serie del dispositivo"),

            // 0x0004 — Tipo scheda (R/W="N" nel CSV, ma attiva)
            Var(dictionary.Id, "Tipo scheda", 0x04,
                DataTypeKind.Other, "Enum dipendente dalle macchine",
                AccessMode.ReadOnly, isEnabled: true,
                description: "Tipo scheda, enumerazione dipendente dal dispositivo"),

            // 0x0005 — Stato (3*uint32_t → Other)
            Var(dictionary.Id, "Stato", 0x05,
                DataTypeKind.Other, "3 * uint32_t",
                AccessMode.ReadOnly,
                description: "Stato della macchina a stati in cui si trova il dispositivo. Interpretazioni words/bits definite per-device"),

            // 0x0006 — Allarmi (Bitmapped[2], WordSize=16, bit interpretations vuote)
            Var(dictionary.Id, "Allarmi", 0x06,
                DataTypeKind.Bitmapped, "Bitmapped[2]",
                AccessMode.ReadOnly, dataTypeParam: 2, wordSize: 16,
                description: "Word 0: allarmi, Word 1: warnings. Interpretazioni bit definite per-device"),

            // 0x0007 — Comandi (R/W="N" nel CSV, disabilitata globalmente)
            Var(dictionary.Id, "Comandi", 0x07,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Riservata, non utilizzata"),

            // 0x0008 — Temperatura scheda
            Var(dictionary.Id, "Temperatura scheda", 0x08,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly,
                description: "Temperatura del microcontrollore"),

            // 0x0009 — Secondi lavoro motore parziale
            Var(dictionary.Id, "Secondi lavoro motore parziale", 0x09,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite,
                description: "Secondi di lavoro motore, contatore parziale resettabile"),

            // 0x000A — Secondi lavoro motore totale
            Var(dictionary.Id, "Secondi lavoro motore totale", 0x0A,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite,
                description: "Secondi di lavoro motore, contatore totale"),

            // 0x000B — Cicli complessivi parziale
            Var(dictionary.Id, "Cicli complessivi parziale", 0x0B,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite,
                description: "Cicli complessivi, contatore parziale resettabile"),

            // 0x000C — Cicli complessivi totale
            Var(dictionary.Id, "Cicli complessivi totale", 0x0C,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite,
                description: "Cicli complessivi, contatore totale"),

            // 0x000D — Cicli completi eseguiti parziale
            Var(dictionary.Id, "Cicli completi eseguiti parziale", 0x0D,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite,
                description: "Cicli completi eseguiti, contatore parziale resettabile"),

            // 0x000E — Cicli completi eseguiti totale
            Var(dictionary.Id, "Cicli completi eseguiti totale", 0x0E,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite,
                description: "Cicli completi eseguiti, contatore totale"),

            // 0x000F — Livello batteria
            Var(dictionary.Id, "Livello batteria", 0x0F,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly,
                description: "Livello di carica della batteria del veicolo"),

            // 0x0010 — Salute batteria (disabilitata globalmente)
            Var(dictionary.Id, "Salute batteria", 0x10,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Stato di salute (SOH) della batteria"),

            // 0x0011 — Cicli batteria (disabilitata globalmente)
            Var(dictionary.Id, "Cicli batteria", 0x11,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Numero di cicli di carica/scarica della batteria"),

            // 0x0012 — Temperatura batteria (disabilitata globalmente)
            Var(dictionary.Id, "Temperatura batteria", 0x12,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Temperatura della batteria in decimi di grado (÷10 per °C)"),

            // 0x0013 — BatteryFirmware (disabilitata globalmente)
            Var(dictionary.Id, "BatteryFirmware", 0x13,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Versione firmware della batteria"),

            // 0x0014 — BatterySerial (disabilitata globalmente)
            Var(dictionary.Id, "BatterySerial", 0x14,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Numero seriale della batteria"),

            // 0x0015 — Stato ingressi fisici (Bitmapped[1], WordSize=32)
            Var(dictionary.Id, "Stato ingressi fisici", 0x15,
                DataTypeKind.Bitmapped, "Bitmapped[1]",
                AccessMode.ReadOnly, dataTypeParam: 1, wordSize: 32,
                description: "Stato degli ingressi fisici della scheda. Interpretazioni bit definite per-device."),

            // 0x0016 — Stato uscite fisiche (Bitmapped[1], WordSize=32)
            Var(dictionary.Id, "Stato uscite fisiche", 0x16,
                DataTypeKind.Bitmapped, "Bitmapped[1]",
                AccessMode.ReadOnly, dataTypeParam: 1, wordSize: 32,
                description: "Stato delle uscite fisiche della scheda. Interpretazioni bit definite per-device."),

            // 0x0017 — Firmware Bootloader
            Var(dictionary.Id, "Firmware Bootloader", 0x17,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, format: "255.255", min: 0, max: 255.255,
                description: "Versione firmware del bootloader"),
        };
        context.Variables.AddRange(variables);

        await context.SaveChangesAsync();

        return variables;
    }

    /// <summary>
    /// Crea il dizionario pulsantiere con le variabili specifiche delle tastiere STEM.
    /// Fonte: Docs/Dictionaries/pulsantiere.CSV
    /// AddressHigh = 0x80 per tutte le variabili pulsantiere.
    /// Aggiorna le BoardEntity pulsantiera per puntare a questo dizionario.
    /// </summary>
    private static async Task<DictionaryEntity> SeedPulsantiereDictionaryAsync(
        AppDbContext context, BoardEntity[] boards)
    {
        var dictionary = new DictionaryEntity
        {
            Name = "Pulsantiere",
            Description = "Dizionario variabili logiche per tastiere esterne STEM",
            IsStandard = false
        };
        context.Dictionaries.Add(dictionary);
        await context.SaveChangesAsync();

        var variables = new[]
        {
            // 0x8000 — Foto Tasti
            Var(dictionary.Id, "Foto Tasti", 0x80, 0x00,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly,
                description: "Variabile logica gestita dalla tastiera esterna"),

            // 0x8001 — Stato sistema (non più usato, disabilitata)
            Var(dictionary.Id, "Stato sistema", 0x80, 0x01,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1, isEnabled: false,
                description: "0 = piano fermo, 1 = piano in movimento. Non più utilizzata."),

            // 0x8002 — Comando Led Verde (Bitmapped[4], WordSize=8)
            Var(dictionary.Id, "Comando Led Verde", 0x80, 0x02,
                DataTypeKind.Bitmapped, "Bitmapped[4]",
                AccessMode.ReadWrite, dataTypeParam: 4, wordSize: 8,
                description: "Pattern lampeggio LED verde. \nWord 0: tempo off tra cicli di lampeggi (4ms). " +
                            "\nWord 1: tempo OFF tra lampeggi (4ms). \nWord 2: tempo ON tra lampeggi (4ms). \nWord 3: attivazione LED, numero di ripetizioni"),

            // 0x8003 — Comando Led Rosso (Bitmapped[4], WordSize=8)
            Var(dictionary.Id, "Comando Led Rosso", 0x80, 0x03,
                DataTypeKind.Bitmapped, "Bitmapped[4]",
                AccessMode.ReadWrite, dataTypeParam: 4, wordSize: 8,
                description: "Pattern lampeggio LED rosso. \nWord 0: tempo off tra cicli di lampeggi (4ms). " +
                            "\nWord 1: tempo OFF tra lampeggi (4ms). \nWord 2: tempo ON tra lampeggi (4ms). \nWord 3: attivazione LED, numero di ripetizioni"),

            // 0x8004 — Comando Buzzer (Bitmapped[4], WordSize=8)
            Var(dictionary.Id, "Comando Buzzer", 0x80, 0x04,
                DataTypeKind.Bitmapped, "Bitmapped[4]",
                AccessMode.ReadWrite, dataTypeParam: 4, wordSize: 8,
                description: "Pattern suono buzzer. \nWord 0: tempo off tra cicli di lampeggi (4ms). " +
                            "\nWord 1: tempo OFF tra lampeggi (4ms). \nWord 2: tempo ON tra lampeggi (4ms). \nWord 3: attivazione buzzer, numero di ripetizioni"),

            // 0x8005 — Beep tasti
            Var(dictionary.Id, "Beep tasti", 0x80, 0x05,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "Abilitazione beep alla pressione dei tasti. 0 = off, 1 = on"),
        };
        context.Variables.AddRange(variables);
        await context.SaveChangesAsync();

        // === BitInterpretations per Comando Led Verde / Rosso / Buzzer ===
        // Big-endian: Word 3 = BYTE 0 (attivazione, bit interpretati)
        // Word 0/1/2: valori interi con interpretazione a bit 0
        var ledVerde = variables[2];
        var ledRosso = variables[3];
        var buzzer = variables[4];

        var bitInterpretations = new List<BitInterpretationEntity>();

        foreach (var varId in new[] { ledVerde.Id, ledRosso.Id, buzzer.Id })
        {
            bitInterpretations.AddRange(TimingWordBits(varId));
        }

        // Led Verde — Word 3 (attivazione)
        bitInterpretations.AddRange(LedBits(ledVerde.Id, 3));
        // Led Rosso — Word 3 (attivazione)
        bitInterpretations.AddRange(LedBits(ledRosso.Id, 3));
        // Buzzer — Word 3 (attivazione, senza bit 2 "single shot")
        bitInterpretations.AddRange(BuzzerBits(buzzer.Id, 3));

        context.Set<BitInterpretationEntity>().AddRange(bitInterpretations);

        // Aggiorna le board pulsantiera per puntare a questo dizionario.
        // Tutte le board con FirmwareType=4 sono pulsantiere (FW=4 nel protocollo STEM)
        // più le pulsantiere TopLift-A2 con FirmwareType=15.
        foreach (var board in boards)
        {
            if (board.FirmwareType is 4 or 15
                && board.Name.Contains("Pulsantiera", StringComparison.OrdinalIgnoreCase))
            {
                board.DictionaryId = dictionary.Id;
            }
        }

        await context.SaveChangesAsync();

        return dictionary;
    }

    /// <summary>
    /// Override variabili standard per il dizionario Pulsantiere.
    /// Le pulsantiere usano solo Firmware macchina (0x0000) e Firmware scheda (0x0001).
    /// Tutte le altre variabili standard vengono disattivate.
    /// </summary>
    private static async Task SeedPulsantiereStandardOverridesAsync(
        AppDbContext context,
        DictionaryEntity pulsantiereDictionary,
        VariableEntity[] standardVariables)
    {
        var overrides = standardVariables
            .Where(v => v.AddressLow is not 0x00 and not 0x01)
            .Select(v => new StandardVariableOverrideEntity
            {
                DictionaryId = pulsantiereDictionary.Id,
                StandardVariableId = v.Id,
                IsEnabled = false,
            })
            .ToArray();

        context.StandardVariableOverrides.AddRange(overrides);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea il dizionario Display Spyke con le variabili specifiche della scheda Display.
    /// Fonte: Docs/Dictionaries/hmi_spyke.CSV
    /// Board: Spyke "Display" (FW=8, MC=5, BoardNumber=1).
    /// 34 variabili device-specific (0x80xx).
    /// </summary>
    private static async Task<DictionaryEntity> SeedDisplaySpykeDictionaryAsync(
        AppDbContext context, BoardEntity[] boards, DeviceEntity spykeDevice)
    {
        var dictionary = new DictionaryEntity
        {
            Name = "Display Spyke",
            Description = "Dizionario variabili logiche scheda Display Spyke",
            IsStandard = false
        };
        context.Dictionaries.Add(dictionary);
        await context.SaveChangesAsync();

        var id = dictionary.Id;
        var variables = new[]
        {
            // 0x8000 — Stato pulsanti
            Var(id, "Stato pulsanti", 0x80, 0x00,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 0, max: 3,
                description: "0 = Nessun pulsante\n1 = Pulsante UP\n"
                    + "2 = Pulsante DOWN\n3 = Entrambi"),

            // 0x8001 — Sensore leva
            Var(id, "Sensore leva", 0x80, 0x01,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "1 = Leva tirata"),

            // 0x8002 — Angolo X
            Var(id, "Angolo X", 0x80, 0x02,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8003 — Angolo Y
            Var(id, "Angolo Y", 0x80, 0x03,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8004 — Angolo Z
            Var(id, "Angolo Z", 0x80, 0x04,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8005 — Accelerazione X
            Var(id, "Accelerazione X", 0x80, 0x05,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8006 — Accelerazione Y
            Var(id, "Accelerazione Y", 0x80, 0x06,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8007 — Accelerazione Z
            Var(id, "Accelerazione Z", 0x80, 0x07,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8008 — Coordinata X touch
            Var(id, "Coordinata X touch", 0x80, 0x08,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8009 — Coordinata Y touch
            Var(id, "Coordinata Y touch", 0x80, 0x09,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x800A — Gancio 10g
            Var(id, "Gancio 10g", 0x80, 0x0A,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "1 = Agganciato"),

            // 0x800B — Gancio Barella
            Var(id, "Gancio Barella", 0x80, 0x0B,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "1 = Agganciato"),

            // 0x800C — Sensore Sherpa
            Var(id, "Sensore Sherpa", 0x80, 0x0C,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "1 = Agganciato"),

            // 0x800D — Tipo Barella
            Var(id, "Tipo Barella", 0x80, 0x0D,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly,
                description: "0 = Emergency stretcher\n1 = Bio stretcher\n"
                    + "2 = Bariatric stretcher\n3 = Incubator"),

            // 0x800E — Stato gateway
            Var(id, "Stato gateway", 0x80, 0x0E,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly,
                description: "0 = not connected\n1 = connected"),

            // 0x800F — RSSI LTE
            Var(id, "RSSI LTE", 0x80, 0x0F,
                DataTypeKind.Int8, "Int8", AccessMode.ReadOnly),

            // 0x8010 — RSSI BLE
            Var(id, "RSSI BLE", 0x80, 0x10,
                DataTypeKind.Int8, "Int8", AccessMode.ReadOnly),

            // 0x8011 — Stato connessione Sherpa
            Var(id, "Stato connessione Sherpa", 0x80, 0x11,
                DataTypeKind.Bool, "Bool", AccessMode.ReadOnly),

            // 0x8012 — Stato automa principale
            Var(id, "Stato automa principale", 0x80, 0x12,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly,
                description: "0 = hooked 10g\n1 = unhooked 10g\n"
                    + "2 = idle out\n3 = ongoing out\n"
                    + "4 = idle mode in\n5 = ongoing in\n6 = extracted"),

            // 0x8013 — Tensione batteria mV
            Var(id, "Tensione batteria mV", 0x80, 0x13,
                DataTypeKind.UInt32, "UInt32", AccessMode.ReadOnly),

            // 0x8014 — Stato ricarica
            Var(id, "Stato ricarica", 0x80, 0x14,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly,
                description: "1 = in ricarica"),

            // 0x8015 — Livello batteria soglie
            Var(id, "Livello batteria soglie", 0x80, 0x15,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly,
                description: "0=100%\n1=80%\n2=60%\n3=40%\n"
                    + "4=20%\n5=warning\n6=alarm"),

            // 0x8016 — Tempo unix
            Var(id, "Tempo unix", 0x80, 0x16,
                DataTypeKind.UInt32, "UInt32", AccessMode.ReadOnly),

            // 0x8017 — Firmware HMI
            Var(id, "Firmware HMI", 0x80, 0x17,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, format: "255.255"),

            // 0x8018 — Abilitazione programmazione Sherpa
            Var(id, "Abilitazione programmazione Sherpa", 0x80, 0x18,
                DataTypeKind.Bool, "Bool", AccessMode.ReadOnly),

            // 0x8019 — Gateway loading file
            Var(id, "Gateway loading file", 0x80, 0x19,
                DataTypeKind.Bool, "Bool", AccessMode.ReadOnly),

            // 0x801A — Gateway loading file size
            Var(id, "Gateway loading file size", 0x80, 0x1A,
                DataTypeKind.UInt32, "UInt32", AccessMode.ReadOnly),

            // 0x801B — Gateway loading file transfered
            Var(id, "Gateway loading file transfered", 0x80, 0x1B,
                DataTypeKind.UInt32, "UInt32", AccessMode.ReadOnly),

            // 0x801C — Gateway firmware version
            Var(id, "Gateway firmware version", 0x80, 0x1C,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, format: "255.255"),

            // 0x801D — Angolo X offset
            Var(id, "Angolo X offset", 0x80, 0x1D,
                DataTypeKind.Float, "Float", AccessMode.ReadWrite),

            // 0x801E — Angolo Y offset
            Var(id, "Angolo Y offset", 0x80, 0x1E,
                DataTypeKind.Float, "Float", AccessMode.ReadWrite),

            // 0x801F — Angolo Z offset
            Var(id, "Angolo Z offset", 0x80, 0x1F,
                DataTypeKind.Float, "Float", AccessMode.ReadWrite),

            // 0x8020 — Angolo theta
            Var(id, "Angolo theta", 0x80, 0x20,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x8021 — Azzeramento posizione angoli
            Var(id, "Azzeramento posizione angoli", 0x80, 0x21,
                DataTypeKind.UInt8, "UInt8", AccessMode.ReadWrite),
        };
        context.Variables.AddRange(variables);

        // Link board Display (FW=8) di Spyke
        foreach (var board in boards)
        {
            if (board.DeviceId == spykeDevice.Id && board.FirmwareType == 8)
                board.DictionaryId = dictionary.Id;
        }

        await context.SaveChangesAsync();

        return dictionary;
    }

    /// <summary>
    /// Override variabili standard per il dizionario Display Spyke.
    /// - Disabilita: Temperatura scheda (0x08), Secondi motore parziale (0x09), totale (0x0A).
    /// - Descrizione: Cicli 0x0B-0x0E con testo specifico dal CSV.
    /// - BitInterpretation per-dizionario: Allarmi (0x06) Word 0 + Word 1.
    /// </summary>
    private static async Task SeedDisplaySpykeOverridesAsync(
        AppDbContext context,
        DictionaryEntity displaySpykeDictionary,
        VariableEntity[] standardVariables)
    {
        var dictId = displaySpykeDictionary.Id;

        // === Override IsEnabled ===
        // Disabilita Temperatura scheda (0x08), Secondi motore parziale/totale (0x09, 0x0A)
        var disabledOverrides = standardVariables
            .Where(v => v.AddressLow is 0x08 or 0x09 or 0x0A)
            .Select(v => new StandardVariableOverrideEntity
            {
                DictionaryId = dictId,
                StandardVariableId = v.Id,
                IsEnabled = false,
            });

        // === Override Descrizione Cicli ===
        var cicliDescriptions = new Dictionary<byte, string>
        {
            [0x0B] = "Numero agganci al 10G resettabile "
                + "(il segnale arriva dal Gateway ma l'elaborazione "
                + "come significato e funzione avviene qui)",
            [0x0C] = "Numero agganci al 10G non resettabile",
            [0x0D] = "Numero agganci al 10G resettabile "
                + "dopo ciclo Sherpa?",
            [0x0E] = "Numero agganci al 10G non resettabile "
                + "dopo ciclo Sherpa?",
        };
        var cicliOverrides = standardVariables
            .Where(v => cicliDescriptions.ContainsKey(v.AddressLow))
            .Select(v => new StandardVariableOverrideEntity
            {
                DictionaryId = dictId,
                StandardVariableId = v.Id,
                IsEnabled = true,
                Description = cicliDescriptions[v.AddressLow],
            });

        context.StandardVariableOverrides.AddRange(
            disabledOverrides.Concat(cicliOverrides));
        await context.SaveChangesAsync();

        // === BitInterpretation per-dizionario per Allarmi (0x06) ===
        var allarmi = standardVariables.First(v => v.AddressLow == 0x06);
        var bits = new BitInterpretationEntity[]
        {
            // Word 0: Allarmi
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "Errore CAN" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 1, Meaning = "Tensione troppo bassa" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 2, Meaning = "Errore touch" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 3,
                Meaning = "Errore sensore 10G (se vedo Vricarica senza vedere 10G)" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 4,
                Meaning = "Sovraccarico celle (quando sono a tensione batteria "
                    + "massima ma ho ancora corrente)" },

            // Word 1: Avvisi
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 0, Meaning = "Tensione bassa" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 1,
                Meaning = "NFC non presente (se barellino agganciato)" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 2,
                Meaning = "NFC non riconosciuto (se barellino agganciato)" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 3,
                Meaning = "Barellino non agganciato (se NFC presente)" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 4,
                Meaning = "Mancanza di Vricarica con 10G agganciato" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 5,
                Meaning = "Celle sbilanciate (quando sono in ricarica "
                    + "ma non ho corrente di ricarica)" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 6,
                Meaning = "Perdita di stabilità della barella (da giroscopio)" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 7,
                Meaning = "Incoerenza leva/pulsante in carico" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 8,
                Meaning = "Incoerenza leva/pulsante in scarico" },
        };
        context.Set<BitInterpretationEntity>().AddRange(bits);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea il dizionario Gateway Spyke con le variabili specifiche della scheda Gateway.
    /// Fonte: Docs/Dictionaries/gateway_spyke.CSV
    /// Board: Spyke "Gateway" (FW=7, MC=5, BoardNumber=1).
    /// 9 variabili device-specific (0x80xx).
    /// </summary>
    private static async Task<DictionaryEntity> SeedGatewaySpykeDictionaryAsync(
        AppDbContext context, BoardEntity[] boards, DeviceEntity spykeDevice)
    {
        var dictionary = new DictionaryEntity
        {
            Name = "Gateway Spyke",
            Description = "Dizionario variabili logiche scheda Gateway Spyke",
            IsStandard = false
        };
        context.Dictionaries.Add(dictionary);
        await context.SaveChangesAsync();

        var id = dictionary.Id;
        var variables = new[]
        {
            // 0x8000 — Gancio 10G
            Var(id, "Gancio 10G", 0x80, 0x00,
                DataTypeKind.UInt8, "UInt8", AccessMode.ReadOnly),

            // 0x8001 — Gancio barella
            Var(id, "Gancio barella", 0x80, 0x01,
                DataTypeKind.UInt8, "UInt8", AccessMode.ReadOnly),

            // 0x8002 — Sensore Sherpa
            Var(id, "Sensore Sherpa", 0x80, 0x02,
                DataTypeKind.UInt8, "UInt8", AccessMode.ReadOnly),

            // 0x8003 — NFC
            Var(id, "NFC", 0x80, 0x03,
                DataTypeKind.UInt8, "UInt8", AccessMode.ReadOnly),

            // 0x8004 — Luci
            Var(id, "Luci", 0x80, 0x04,
                DataTypeKind.UInt8, "UInt8", AccessMode.ReadWrite),

            // 0x8005 — Dati SIM
            Var(id, "Dati SIM", 0x80, 0x05,
                DataTypeKind.Other, "Custom", AccessMode.ReadWrite),

            // 0x8006 — Stato Gateway
            Var(id, "Stato Gateway", 0x80, 0x06,
                DataTypeKind.Other, "Custom", AccessMode.ReadOnly),

            // 0x8007 — Stato BLE
            Var(id, "Stato BLE", 0x80, 0x07,
                DataTypeKind.Other, "Custom", AccessMode.ReadOnly,
                isEnabled: false),

            // 0x8008 — Stato LTE
            Var(id, "Stato LTE", 0x80, 0x08,
                DataTypeKind.Other, "Custom", AccessMode.ReadOnly,
                isEnabled: false),
        };
        context.Variables.AddRange(variables);

        // Link board Gateway (FW=7) di Spyke
        foreach (var board in boards)
        {
            if (board.DeviceId == spykeDevice.Id && board.FirmwareType == 7)
                board.DictionaryId = dictionary.Id;
        }

        await context.SaveChangesAsync();

        return dictionary;
    }

    /// <summary>
    /// Override variabili standard per il dizionario Gateway Spyke.
    /// - Disabilita: 0x08-0x0F (Temperatura, Secondi motore, Cicli, Livello batteria)
    ///               e 0x17 (Firmware Bootloader).
    /// - BitInterpretation per-dizionario: Allarmi (0x06) Word 0 (5 bit).
    /// </summary>
    private static async Task SeedGatewaySpykeOverridesAsync(
        AppDbContext context,
        DictionaryEntity gatewaySpykeDictionary,
        VariableEntity[] standardVariables)
    {
        var dictId = gatewaySpykeDictionary.Id;

        // === Override IsEnabled ===
        // Disabilita 0x08-0x0F + 0x17
        var disabledOverrides = standardVariables
            .Where(v => (v.AddressLow >= 0x08 && v.AddressLow <= 0x0F)
                || v.AddressLow == 0x17)
            .Select(v => new StandardVariableOverrideEntity
            {
                DictionaryId = dictId,
                StandardVariableId = v.Id,
                IsEnabled = false,
            });

        context.StandardVariableOverrides.AddRange(disabledOverrides);
        await context.SaveChangesAsync();

        // === BitInterpretation per-dizionario per Allarmi (0x06) ===
        var allarmi = standardVariables.First(v => v.AddressLow == 0x06);
        var bits = new BitInterpretationEntity[]
        {
            // Word 0: Allarmi
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "Errore CAN" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 1, Meaning = "NFC non risponde" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 2, Meaning = "Mancanza SIM" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 3,
                Meaning = "Modulo IoT non risponde" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 4,
                Meaning = "Modulo BLE non risponde" },
        };
        context.Set<BitInterpretationEntity>().AddRange(bits);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea il dizionario Gradino con le variabili specifiche della scheda Azionamento.
    /// Fonte: Docs/Dictionaries/gradino.CSV
    /// Board: Gradino "Azionamento" (FW=6, MC=4, BoardNumber=1).
    /// 35 variabili device-specific (0x80xx).
    /// </summary>
    private static async Task<DictionaryEntity> SeedGradinoDictionaryAsync(
        AppDbContext context, BoardEntity[] boards, DeviceEntity gradinoDevice)
    {
        var dictionary = new DictionaryEntity
        {
            Name = "Azionamento Gradino",
            Description = "Dizionario variabili logiche scheda Azionamento Gradino",
            IsStandard = false
        };
        context.Dictionaries.Add(dictionary);
        await context.SaveChangesAsync();

        var id = dictionary.Id;
        var variables = new[]
        {
            // 0x8000 — Stato keyboard 1 (R/W="N" nel CSV, disabilitata)
            Var(id, "Stato keyboard 1", 0x80, 0x00,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Variabile logica gestita dalla tastiera esterna "
                    + "(non usare con l'app)"),

            // 0x8001 — StartLearn
            Var(id, "StartLearn", 0x80, 0x01,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = normale, 1 = vai in apprendimento"),

            // 0x8002 — Posizione
            Var(id, "Posizione", 0x80, 0x02,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -32767, max: 32767, unit: "impulsi",
                description: "Posizione attuale in impulsi"),

            // 0x8003 — Position PID KP
            Var(id, "Position PID KP", 0x80, 0x03,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadWrite, min: -32767, max: 32767),

            // 0x8004 — Position PID KI
            Var(id, "Position PID KI", 0x80, 0x04,
                DataTypeKind.Int32, "Int32",
                AccessMode.ReadWrite, min: -2147483647, max: 2147483647),

            // 0x8005 — I Motore
            Var(id, "I Motore", 0x80, 0x05,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100",
                description: "Corrente del motore"),

            // 0x8006 — Kp I PID
            Var(id, "Kp I PID", 0x80, 0x06,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadWrite, min: -32767, max: 32767),

            // 0x8007 — Ki I PID
            Var(id, "Ki I PID", 0x80, 0x07,
                DataTypeKind.Int32, "Int32",
                AccessMode.ReadWrite, min: -2147483647, max: 2147483647),

            // 0x8008 — Modo FS
            Var(id, "Modo FS", 0x80, 0x08,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = Normale solo contatto porta, 1 = Modo FS"),

            // 0x8009 — Cicli complessivi parziale in Apertura
            Var(id, "Cicli complessivi parziale in Apertura", 0x80, 0x09,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x800A — Cicli complessivi totale in Apertura
            Var(id, "Cicli complessivi totale in Apertura", 0x80, 0x0A,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x800B — Cicli completi eseguiti parziale in Apertura
            Var(id, "Cicli completi eseguiti parziale in Apertura", 0x80, 0x0B,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x800C — Cicli completi eseguiti totale in Apertura
            Var(id, "Cicli completi eseguiti totale in Apertura", 0x80, 0x0C,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x800D — Cicli complessivi parziale in Chiusura
            Var(id, "Cicli complessivi parziale in Chiusura", 0x80, 0x0D,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x800E — Cicli complessivi totale in Chiusura
            Var(id, "Cicli complessivi totale in Chiusura", 0x80, 0x0E,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x800F — Cicli completi eseguiti parziale in Chiusura
            Var(id, "Cicli completi eseguiti parziale in Chiusura", 0x80, 0x0F,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x8010 — Cicli completi eseguiti totale in Chiusura
            Var(id, "Cicli completi eseguiti totale in Chiusura", 0x80, 0x10,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967294),

            // 0x8011 — I max in Apertura
            Var(id, "I max in Apertura", 0x80, 0x11,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8012 — I media in Apertura
            Var(id, "I media in Apertura", 0x80, 0x12,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8013 — I max in prima Apertura
            Var(id, "I max in prima Apertura", 0x80, 0x13,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8014 — I media in prima Apertura
            Var(id, "I media in prima Apertura", 0x80, 0x14,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8015 — I max in Chiusura
            Var(id, "I max in Chiusura", 0x80, 0x15,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8016 — I media in Chiusura
            Var(id, "I media in Chiusura", 0x80, 0x16,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8017 — I max in prima Chiusura
            Var(id, "I max in prima Chiusura", 0x80, 0x17,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8018 — I media in prima Chiusura
            Var(id, "I media in prima Chiusura", 0x80, 0x18,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, min: -800, max: 800, unit: "Ampere/100"),

            // 0x8019 — Salva i valori di prima apertura/chiusura (Bitmapped[1], WordSize=8)
            Var(id, "Salva i valori di prima apertura/chiusura", 0x80, 0x19,
                DataTypeKind.Bitmapped, "Bitmapped[1]",
                AccessMode.ReadWrite, dataTypeParam: 1, wordSize: 8),

            // 0x801A — Step Type (Enum)
            Var(id, "Step Type", 0x80, 0x1A,
                DataTypeKind.Other, "Enum",
                AccessMode.ReadWrite, min: 0, max: 5,
                description: "0 = GE2\n1 = GE3\n2 = GE4\n"
                    + "3 = DRAWER\n4 = SHED\n5 = BENCH"),

            // 0x801B — Velocità LOW (% di NORMAL)
            Var(id, "Velocità LOW", 0x80, 0x1B,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadWrite, min: 0, max: 100, unit: "%"),

            // 0x801C — Max current (abs)
            Var(id, "Max current", 0x80, 0x1C,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, unit: "Ampere/100"),

            // 0x801D — Max current primo zero (abs)
            Var(id, "Max current primo zero", 0x80, 0x1D,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, unit: "Ampere/100"),

            // 0x801E — Max currentzero (abs)
            Var(id, "Max currentzero", 0x80, 0x1E,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, unit: "Ampere/100"),

            // 0x801F — Motor Type (Enum, disabilitata, senza indirizzo nel CSV)
            Var(id, "Motor Type", 0x80, 0x1F,
                DataTypeKind.Other, "Enum",
                AccessMode.ReadWrite, min: 0, max: 4, isEnabled: false,
                description: "0 = Not initialized\n1 = DC BRUSHLESS\n"
                    + "2 = DC\n3 = AC INDUCTION\n4 = AC BRUSHLESS"),

            // 0x8020 — Stato Scheda (disabilitata, senza indirizzo nel CSV)
            Var(id, "Stato Scheda", 0x80, 0x20,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8021 — An_Pot1 (disabilitata, debug, senza indirizzo nel CSV)
            Var(id, "An_Pot1", 0x80, 0x21,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, min: 0, max: 4095, unit: "Bit",
                isEnabled: false),

            // 0x8022 — An_Pot2 (disabilitata, debug, senza indirizzo nel CSV)
            Var(id, "An_Pot2", 0x80, 0x22,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, min: 0, max: 4095, unit: "Bit",
                isEnabled: false),
        };
        context.Variables.AddRange(variables);
        await context.SaveChangesAsync();

        // === BitInterpretations per Salva i valori (0x8019) ===
        var salvaValori = variables.First(v => v.AddressLow == 0x19);
        var salvaBits = new BitInterpretationEntity[]
        {
            new() { VariableId = salvaValori.Id, WordIndex = 0, BitIndex = 0,
                Meaning = "Salva i valori alla prossima apertura" },
            new() { VariableId = salvaValori.Id, WordIndex = 0, BitIndex = 1,
                Meaning = "Salva i valori alla prossima chiusura" },
            new() { VariableId = salvaValori.Id, WordIndex = 0, BitIndex = 2,
                Meaning = "Salva i valori alla fine del prossimo apprendimento" },
        };
        context.Set<BitInterpretationEntity>().AddRange(salvaBits);

        // Link board Azionamento (FW=6) di Gradino
        foreach (var board in boards)
        {
            if (board.DeviceId == gradinoDevice.Id && board.FirmwareType == 6)
                board.DictionaryId = dictionary.Id;
        }

        await context.SaveChangesAsync();

        return dictionary;
    }

    /// <summary>
    /// Override variabili standard per il dizionario Gradino.
    /// - Disabilita: 0x05, 0x07-0x0F, 0x17 (11 variabili).
    /// - Descrizione: 0x05 (Stato) con enum macchina a stati.
    /// - BitInterpretation per-dizionario: 0x15 (Ingressi) 4 bit, 0x16 (Uscite) 1 bit.
    /// </summary>
    private static async Task SeedGradinoOverridesAsync(
        AppDbContext context,
        DictionaryEntity gradinoDictionary,
        VariableEntity[] standardVariables)
    {
        var dictId = gradinoDictionary.Id;

        // === Override IsEnabled + Descrizione ===
        var overrides = new List<StandardVariableOverrideEntity>();

        // Disabilita 0x05 (Stato) con descrizione enum
        var stato = standardVariables.First(v => v.AddressLow == 0x05);
        overrides.Add(new StandardVariableOverrideEntity
        {
            DictionaryId = dictId,
            StandardVariableId = stato.Id,
            IsEnabled = false,
            Description = "00 = UNDEFINED\n01 = OPENING_CALIBRATION\n"
                + "02 = CLOSING_CALIBRATION\n03 = RESETTING\n"
                + "04 = END_CLOSING\n05 = IN_CLOSING\n"
                + "06 = END_OPENING\n07 = IN_OPENING\n"
                + "08 = STOPPED\n09 = IN_FAULT\n"
                + "10 = SLOWDOWN_CLOSING\n11 = SLOWDOWN_OPENING",
        });

        // Disabilita 0x07-0x0F + 0x17
        var disabledAddresses = standardVariables
            .Where(v => (v.AddressLow >= 0x07 && v.AddressLow <= 0x0F)
                || v.AddressLow == 0x17);
        foreach (var v in disabledAddresses)
        {
            overrides.Add(new StandardVariableOverrideEntity
            {
                DictionaryId = dictId,
                StandardVariableId = v.Id,
                IsEnabled = false,
            });
        }

        context.StandardVariableOverrides.AddRange(overrides);
        await context.SaveChangesAsync();

        // === BitInterpretation per-dizionario ===
        var ingressi = standardVariables.First(v => v.AddressLow == 0x15);
        var uscite = standardVariables.First(v => v.AddressLow == 0x16);

        var bits = new BitInterpretationEntity[]
        {
            // 0x0015 — Stato ingressi fisici, Word 0
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "DOOR" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 1, Meaning = "FS OPEN" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 2, Meaning = "FS CLOSE" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 3, Meaning = "FC STEP" },

            // 0x0016 — Stato uscite fisiche, Word 0
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "LED1" },
        };
        context.Set<BitInterpretationEntity>().AddRange(bits);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Crea il dizionario Eden-XP con le variabili specifiche della scheda Madre.
    /// Fonte: Docs/Dictionaries/eden-xp.CSV
    /// Board: Eden-XP "Madre" (FW=5, MC=3, BoardNumber=1).
    /// 130 variabili device-specific (0x80xx).
    /// </summary>
    private static async Task<DictionaryEntity> SeedEdenXPDictionaryAsync(
        AppDbContext context, BoardEntity[] boards, DeviceEntity edenXPDevice)
    {
        var dictionary = new DictionaryEntity
        {
            Name = "Madre Eden-XP",
            Description = "Dizionario variabili logiche scheda Madre Eden-XP",
            IsStandard = false
        };
        context.Dictionaries.Add(dictionary);
        await context.SaveChangesAsync();

        var id = dictionary.Id;
        var variables = new[]
        {
            // 0x8000 — Stato keyboard 1 (R/W="N" nel CSV, disabilitata)
            Var(id, "Stato keyboard 1", 0x80, 0x00,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Variabile logica gestita dalla tastiera esterna (non usare con l'app)"),

            // 0x8001 — SystemOn
            Var(id, "SystemOn", 0x80, 0x01,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = piano spento, 1 = piano acceso"),

            // 0x8002 — Angolo inclinazione del piano
            Var(id, "Angolo inclinazione del piano", 0x80, 0x02,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi",
                description: "Angolo di inclinazione del piano: "
                    + "12.0 è testa su massimo, -12.0 è piedi su massimo, 0.0 è orizzontale"),

            // 0x8003 — Angolo lato testa
            Var(id, "Angolo lato testa", 0x80, 0x03,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi",
                description: "Angolo di inclinazione calcolato lato testa (si usa a piano chiuso)"),

            // 0x8004 — Tastiera SherpaSlim
            Var(id, "Tastiera SherpaSlim", 0x80, 0x04,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly,
                description: "Immagine tasti SherpaSlim per gestire i 2 blu"),

            // 0x8005 — Potenzio inclinazione min
            Var(id, "Potenzio inclinazione min", 0x80, 0x05,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 0, max: 65535, unit: "bits",
                description: "Valore minimo in bits letto dal valore RAW del potenzio in apprendimento"),

            // 0x8006 — Potenzio inclinazione max
            Var(id, "Potenzio inclinazione max", 0x80, 0x06,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 0, max: 65535, unit: "bits",
                description: "Valore massimo in bits letto dal valore RAW del potenzio in apprendimento"),

            // 0x8007 — Potenzio altezza min
            Var(id, "Potenzio altezza min", 0x80, 0x07,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 0, max: 65535, unit: "bits",
                description: "Valore minimo in bits letto dal valore RAW del potenzio in apprendimento"),

            // 0x8008 — Potenzio altezza max
            Var(id, "Potenzio altezza max", 0x80, 0x08,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 0, max: 65535, unit: "bits",
                description: "Valore massimo in bits letto dal valore RAW del potenzio in apprendimento"),

            // 0x8009 — Angolo X
            Var(id, "Angolo X", 0x80, 0x09,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, min: -180, max: 180, unit: "gradi"),

            // 0x800A — Angolo Y
            Var(id, "Angolo Y", 0x80, 0x0A,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, min: -180, max: 180, unit: "gradi"),

            // 0x800B — Angolo Z
            Var(id, "Angolo Z", 0x80, 0x0B,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, min: -180, max: 180, unit: "gradi"),

            // 0x800C — Accelerazione X
            Var(id, "Accelerazione X", 0x80, 0x0C,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x800D — Accelerazione Y
            Var(id, "Accelerazione Y", 0x80, 0x0D,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x800E — Accelerazione Z
            Var(id, "Accelerazione Z", 0x80, 0x0E,
                DataTypeKind.Float, "Float", AccessMode.ReadOnly),

            // 0x800F — Stato finecorsa (Bitmapped[1], WordSize=8)
            Var(id, "Stato finecorsa", 0x80, 0x0F,
                DataTypeKind.Bitmapped, "Bitmapped[1]",
                AccessMode.ReadOnly, dataTypeParam: 1, wordSize: 8),

            // 0x8010 — libero (disabilitata)
            Var(id, "libero", 0x80, 0x10,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8011 — Stato comando esterno su
            Var(id, "Stato comando esterno su", 0x80, 0x11,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1),

            // 0x8012 — Stato comando esterno giù
            Var(id, "Stato comando esterno giù", 0x80, 0x12,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1),

            // 0x8013 — Stato pompa
            Var(id, "Stato pompa", 0x80, 0x13,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),

            // 0x8014 — Pompa Riferimento (disabilitata)
            Var(id, "Pompa Riferimento", 0x80, 0x14,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8015 — Pompa I misurata (disabilitata)
            Var(id, "Pompa I misurata", 0x80, 0x15,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8016 — Pompa PWM Out (disabilitata)
            Var(id, "Pompa PWM Out", 0x80, 0x16,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8017 — I max Pompa (disabilitata)
            Var(id, "I max Pompa", 0x80, 0x17,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV1 (0x8018-0x801B) ===
            Var(id, "Stato EV1", 0x80, 0x18,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV1 I Reference", 0x80, 0x19,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV1 I Measured", 0x80, 0x1A,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV1 PWM out", 0x80, 0x1B,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV2 (0x801C-0x801F) ===
            Var(id, "Stato EV2", 0x80, 0x1C,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV2 I Reference", 0x80, 0x1D,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV2 I Measured", 0x80, 0x1E,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV2 PWM out", 0x80, 0x1F,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV3 (0x8020-0x8023) ===
            Var(id, "Stato EV3", 0x80, 0x20,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV3 I Reference", 0x80, 0x21,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV3 I Measured", 0x80, 0x22,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV3 PWM out", 0x80, 0x23,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV4 (0x8024-0x8027) ===
            Var(id, "Stato EV4", 0x80, 0x24,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV4 I Reference", 0x80, 0x25,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV4 I Measured", 0x80, 0x26,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV4 PWM out", 0x80, 0x27,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV5 (0x8028-0x802B) ===
            Var(id, "Stato EV5", 0x80, 0x28,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV5 I Reference", 0x80, 0x29,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV5 I Measured", 0x80, 0x2A,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV5 PWM out", 0x80, 0x2B,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV6 (0x802C-0x802F) ===
            Var(id, "Stato EV6", 0x80, 0x2C,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV6 I Reference", 0x80, 0x2D,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV6 I Measured", 0x80, 0x2E,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV6 PWM out", 0x80, 0x2F,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV7 (0x8030-0x8033) ===
            Var(id, "Stato EV7", 0x80, 0x30,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV7 I Reference", 0x80, 0x31,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV7 I Measured", 0x80, 0x32,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV7 PWM out", 0x80, 0x33,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV8 (0x8034-0x8037) ===
            Var(id, "Stato EV8", 0x80, 0x34,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV8 I Reference", 0x80, 0x35,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV8 I Measured", 0x80, 0x36,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV8 PWM out", 0x80, 0x37,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV9 (0x8038-0x803B, tutte disabilitate) ===
            Var(id, "Stato EV9", 0x80, 0x38,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2, isEnabled: false,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV9 I Reference", 0x80, 0x39,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV9 I Measured", 0x80, 0x3A,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV9 PWM out", 0x80, 0x3B,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // === EV10 (0x803C-0x803F, tutte disabilitate) ===
            Var(id, "Stato EV10", 0x80, 0x3C,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2, isEnabled: false,
                description: "0 = fermo, 1 = in accensione, 2 = acceso"),
            Var(id, "EV10 I Reference", 0x80, 0x3D,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV10 I Measured", 0x80, 0x3E,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "EV10 PWM out", 0x80, 0x3F,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8040 — Vbus measured (disabilitata)
            Var(id, "Vbus measured", 0x80, 0x40,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8041 — Stato Luci (Bitmapped[1], WordSize=8)
            Var(id, "Stato Luci", 0x80, 0x41,
                DataTypeKind.Bitmapped, "Bitmapped[1]",
                AccessMode.ReadOnly, dataTypeParam: 1, wordSize: 8),

            // === Ore lavoro (0x8042-0x804C, tutte disabilitate, Log) ===
            Var(id, "Ore lavoro pompa", 0x80, 0x42,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV1", 0x80, 0x43,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV2", 0x80, 0x44,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV3", 0x80, 0x45,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV4", 0x80, 0x46,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV5", 0x80, 0x47,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV6", 0x80, 0x48,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV7", 0x80, 0x49,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV8", 0x80, 0x4A,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV9", 0x80, 0x4B,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ore lavoro EV10", 0x80, 0x4C,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x804D — Salva orizzontale
            Var(id, "Salva orizzontale", 0x80, 0x4D,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "se va a 0 prendo lo zero del piano"),

            // === Log diagnostici (0x804E-0x8051, disabilitate) ===
            Var(id, "Numero singoli allarmi", 0x80, 0x4E,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ultimo allarme", 0x80, 0x4F,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Numero singoli warning", 0x80, 0x50,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),
            Var(id, "Ultimo warning", 0x80, 0x51,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, isEnabled: false),

            // 0x8052 — Offset orizzontale in bits
            Var(id, "Offset orizzontale in bits", 0x80, 0x52,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 0, max: 32767, unit: "bits"),

            // 0x8053 — Valore RAW del potenzio altezza
            Var(id, "Valore RAW del potenzio altezza", 0x80, 0x53,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, min: 0, max: 32767, unit: "bits"),

            // 0x8054 — Valore RAW del potenzio inclinazione
            Var(id, "Valore RAW del potenzio inclinazione", 0x80, 0x54,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly, min: 0, max: 32767, unit: "bits"),

            // 0x8055 — Soglia di undervoltage
            Var(id, "Soglia di undervoltage", 0x80, 0x55,
                DataTypeKind.Float, "Float",
                AccessMode.ReadWrite, min: 0.0, max: 14.0, unit: "volts",
                description: "Valore minimo di batteria (a cui ho 0% e scatto del fault)"),

            // 0x8056 — Soglia di batteria carica 100%
            Var(id, "Soglia di batteria carica 100%", 0x80, 0x56,
                DataTypeKind.Float, "Float",
                AccessMode.ReadWrite, min: 0.0, max: 14.0, unit: "volts",
                description: "Valore massimo di batteria (a cui ho il 100%)"),

            // 0x8057 — Altezza testa (disabilitata)
            Var(id, "Altezza testa", 0x80, 0x57,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly, unit: "mm", isEnabled: false,
                description: "Altezza testa in mm"),

            // 0x8058 — Autospegnimento in rigido
            Var(id, "Autospegnimento in rigido", 0x80, 0x58,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = sempre acceso, 1 = autospegnimento"),

            // 0x8059 — Timer autospegni in rigido
            Var(id, "Timer autospegni in rigido", 0x80, 0x59,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 20, max: 600, unit: "min"),

            // 0x805A — Autospegnimento in molleggio
            Var(id, "Autospegnimento in molleggio", 0x80, 0x5A,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = sempre acceso, 1 = autospegnimento"),

            // 0x805B — Timer autospegni in molleggio
            Var(id, "Timer autospegni in molleggio", 0x80, 0x5B,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadWrite, min: 20, max: 600, unit: "min"),

            // 0x805C — Vai in molleggio alla richiusura (disabilitata)
            Var(id, "Vai in molleggio alla richiusura", 0x80, 0x5C,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1, isEnabled: false,
                description: "0 = disattivo, 1 = attivo"),

            // 0x805D — Vai ad altezza di carico all'estrazione
            Var(id, "Vai ad altezza di carico all'estrazione", 0x80, 0x5D,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = disattivo, 1 = attivo"),

            // 0x805E — Vai sempre in molleggio (disabilitata)
            Var(id, "Vai sempre in molleggio", 0x80, 0x5E,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1, isEnabled: false,
                description: "0 = rigido, 1 = molleggio"),

            // 0x805F — Collaudo
            Var(id, "Collaudo", 0x80, 0x5F,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = funzionamento normale, 1 = collaudo"),

            // 0x8060 — Presenza can2 per collaudo
            Var(id, "Presenza can2 per collaudo", 0x80, 0x60,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = non connesso, 1 = can2 presente"),

            // 0x8061 — Colore in Standby
            Var(id, "Colore in Standby", 0x80, 0x61,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 16777215,
                description: "Colore RGB a 24 bit: 0x0RGB"),

            // 0x8062 — Colore in Movimento
            Var(id, "Colore in Movimento", 0x80, 0x62,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 16777215,
                description: "Colore RGB a 24 bit: 0x0RGB"),

            // 0x8063 — Collaudo scheda eseguito
            Var(id, "Collaudo scheda eseguito", 0x80, 0x63,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8064 — Collaudo FC Closed
            Var(id, "Collaudo FC Closed", 0x80, 0x64,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8065 — Collaudo FC Extended
            Var(id, "Collaudo FC Extended", 0x80, 0x65,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8066 — Collaudo Potenzio altezza valore minimo
            Var(id, "Collaudo Potenzio altezza valore minimo", 0x80, 0x66,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8067 — Collaudo Potenzio altezza valore massimo
            Var(id, "Collaudo Potenzio altezza valore massimo", 0x80, 0x67,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8068 — Collaudo Potenzio inclinazione valore minimo
            Var(id, "Collaudo Potenzio inclinazione valore minimo", 0x80, 0x68,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8069 — Collaudo Potenzio inclinazione valore massimo
            Var(id, "Collaudo Potenzio inclinazione valore massimo", 0x80, 0x69,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x806A — Collaudo Leva su
            Var(id, "Collaudo Leva su", 0x80, 0x6A,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x806B — Collaudo Leva giù
            Var(id, "Collaudo Leva giù", 0x80, 0x6B,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x806C — Collaudo CAN 1
            Var(id, "Collaudo CAN 1", 0x80, 0x6C,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x806D — Collaudo CAN 2
            Var(id, "Collaudo CAN 2", 0x80, 0x6D,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x806E — Collaudo costa
            Var(id, "Collaudo costa", 0x80, 0x6E,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, min: 0, max: 2,
                description: "2 = passato altrimenti non passato"),

            // 0x806F — Collaudo pompa e valvole
            Var(id, "Collaudo pompa e valvole", 0x80, 0x6F,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadOnly, min: 0, max: 1,
                description: "0 = non passato, 1 = passato"),

            // 0x8070 — Attivazione Fault Costa
            Var(id, "Attivazione Fault Costa", 0x80, 0x70,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "0 = costa non attiva, 1 = costa attiva"),

            // 0x8071 — Altezza minima molleggio (angolo)
            Var(id, "Altezza minima molleggio", 0x80, 0x71,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi",
                description: "Altezza minima a cui l'Eden scarica quando sta per entrare in molleggio"),

            // 0x8072 — Altezza massima molleggio (angolo)
            Var(id, "Altezza massima molleggio", 0x80, 0x72,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi",
                description: "Altezza cui si posiziona l'Eden in molleggio "
                    + "(se è inclinato è l'equivalente orizzontale)"),

            // 0x8073 — Stato keyboard 2 (R/W="N" nel CSV, disabilitata)
            Var(id, "Stato keyboard 2", 0x80, 0x73,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Variabile logica gestita dalla tastiera esterna (non usare con l'app)"),

            // 0x8074 — Stato keyboard 3 (R/W="N" nel CSV, disabilitata)
            Var(id, "Stato keyboard 3", 0x80, 0x74,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadOnly, isEnabled: false,
                description: "Variabile logica gestita dalla tastiera esterna (non usare con l'app)"),

            // 0x8075 — Angolo lato piedi
            Var(id, "Angolo lato piedi", 0x80, 0x75,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi",
                description: "Angolo di inclinazione calcolato lato piedi"),

            // 0x8076 — Max angolo testa
            Var(id, "Max angolo testa", 0x80, 0x76,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi"),

            // 0x8077 — Min angolo testa
            Var(id, "Min angolo testa", 0x80, 0x77,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi"),

            // 0x8078 — Max angolo inclinazione
            Var(id, "Max angolo inclinazione", 0x80, 0x78,
                DataTypeKind.Float, "Float",
                AccessMode.ReadOnly, unit: "gradi"),

            // 0x8079 — Min angolo inclinazione
            Var(id, "Min angolo inclinazione", 0x80, 0x79,
                DataTypeKind.Float, "Float",
                AccessMode.ReadWrite, unit: "gradi",
                description: "Angolo minimo che si può raggiungere lato piedi\n"
                    + "da estratto: va da 13.8 (tutto inclinato) a 0 (orizzontale)"),

            // 0x807A — Tolleranza fine corsa alto angolo piedi
            Var(id, "Tolleranza fine corsa alto angolo piedi", 0x80, 0x7A,
                DataTypeKind.Float, "Float",
                AccessMode.ReadWrite, unit: "gradi",
                description: "Tolleranza sullo scatto fine corsa in meno rispetto al massimo angolo lato piedi"),

            // 0x807B — Learn Time OK
            Var(id, "Learn Time OK", 0x80, 0x7B,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967295, unit: "ms",
                description: "Tempo in cui devo rimanere fermo per passare "
                    + "da una fase di apprendimento a un'altra"),

            // 0x807C — Learn Time Hide Check
            Var(id, "Learn Time Hide Check", 0x80, 0x7C,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967295, unit: "ms",
                description: "Tempo in cui mi muovo comunque tra una fase di apprendimento "
                    + "e l'altra per funzionare anche con i martinetti più lenti"),

            // 0x807D — Pompa soglia di corrente per aperto
            Var(id, "Pompa soglia di corrente per aperto", 0x80, 0x7D,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadWrite, min: -32767, max: 32767, unit: "bits",
                description: "Soglia di corrente sotto la quale riconosco pompa aperta"),

            // 0x807E — Pompa tempo per aperto
            Var(id, "Pompa tempo per aperto", 0x80, 0x7E,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadWrite, min: 0, max: 4294967295, unit: "ms",
                description: "Tempo con corrente pompa sotto soglia per far scattare il fault"),

            // 0x807F — Livello di luce barra a led
            Var(id, "Livello di luce barra a led", 0x80, 0x7F,
                DataTypeKind.UInt8, "UInt8",
                AccessMode.ReadWrite, min: 0, max: 100, unit: "%",
                description: "Livello di luce della barra a led"),

            // 0x8080 — Virtual keyboard (Bitmapped[1], WordSize=8)
            Var(id, "Virtual keyboard", 0x80, 0x80,
                DataTypeKind.Bitmapped, "Bitmapped[1]",
                AccessMode.ReadWrite, dataTypeParam: 1, wordSize: 8,
                description: "Tastiera virtuale per replicare i tasti tramite app"),

            // 0x8081 — Salva angolo minimo da estratto
            Var(id, "Salva angolo minimo da estratto", 0x80, 0x81,
                DataTypeKind.Bool, "Bool",
                AccessMode.ReadWrite, min: 0, max: 1,
                description: "Se va a 0 salvo come angolo minimo l'attuale inclinazione del piano"),
        };
        context.Variables.AddRange(variables);
        await context.SaveChangesAsync();

        // === BitInterpretations per Stato finecorsa (0x800F) ===
        var statoFinecorsa = variables.First(v => v.AddressLow == 0x0F);
        var finecorsaBits = new BitInterpretationEntity[]
        {
            new() { VariableId = statoFinecorsa.Id, WordIndex = 0, BitIndex = 0,
                Meaning = "Finecorsa piano esteso" },
            new() { VariableId = statoFinecorsa.Id, WordIndex = 0, BitIndex = 1,
                Meaning = "Finecorsa piano chiuso" },
        };
        context.Set<BitInterpretationEntity>().AddRange(finecorsaBits);

        // === BitInterpretations per Stato Luci (0x8041) ===
        var statoLuci = variables.First(v => v.AddressLow == 0x41);
        var luciBits = new BitInterpretationEntity[]
        {
            new() { VariableId = statoLuci.Id, WordIndex = 0, BitIndex = 0, Meaning = "B" },
            new() { VariableId = statoLuci.Id, WordIndex = 0, BitIndex = 1, Meaning = "G" },
            new() { VariableId = statoLuci.Id, WordIndex = 0, BitIndex = 2, Meaning = "R" },
        };
        context.Set<BitInterpretationEntity>().AddRange(luciBits);

        // === BitInterpretations per Virtual keyboard (0x8080) ===
        var virtualKb = variables.First(v => v.AddressLow == 0x80);
        var kbBits = new BitInterpretationEntity[]
        {
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 0, Meaning = "TESTA SU" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 1, Meaning = "PIEDI SU" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 2, Meaning = "ORIZZONTALE" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 3, Meaning = "MOLLEGGIO" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 4, Meaning = "TUTTO SU" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 5, Meaning = "TUTTO GIU" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 6, Meaning = "STOP" },
            new() { VariableId = virtualKb.Id, WordIndex = 0, BitIndex = 7, Meaning = "LUCI" },
        };
        context.Set<BitInterpretationEntity>().AddRange(kbBits);

        // Link board Madre (FW=5) di Eden-XP
        foreach (var board in boards)
        {
            if (board.DeviceId == edenXPDevice.Id && board.FirmwareType == 5)
                board.DictionaryId = dictionary.Id;
        }

        await context.SaveChangesAsync();

        return dictionary;
    }

    /// <summary>
    /// Override variabili standard per il dizionario Eden-XP.
    /// - Disabilita: 0x05 (Stato).
    /// - BitInterpretation per-dizionario: 0x06 (Allarmi) Word 0 (16 bit) + Word 1 (6 bit),
    ///   0x15 (Ingressi) 12 bit, 0x16 (Uscite) 12 bit.
    /// </summary>
    private static async Task SeedEdenXPOverridesAsync(
        AppDbContext context,
        DictionaryEntity edenXPDictionary,
        VariableEntity[] standardVariables)
    {
        var dictId = edenXPDictionary.Id;

        // === Override IsEnabled: Disabilita 0x05 (Stato) ===
        var disabledOverrides = standardVariables
            .Where(v => v.AddressLow == 0x05)
            .Select(v => new StandardVariableOverrideEntity
            {
                DictionaryId = dictId,
                StandardVariableId = v.Id,
                IsEnabled = false,
            });
        context.StandardVariableOverrides.AddRange(disabledOverrides);
        await context.SaveChangesAsync();

        // === BitInterpretation per-dizionario per Allarmi (0x06) ===
        var allarmi = standardVariables.First(v => v.AddressLow == 0x06);
        var allarmiBits = new BitInterpretationEntity[]
        {
            // Word 0: Allarmi
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "Sovracorrente pompa" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 1, Meaning = "Circuito aperto pompa" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 2, Meaning = "Sovracorrente EV 1" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 3, Meaning = "Circuito aperto EV 1" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 4, Meaning = "Sovracorrente EV 2" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 5, Meaning = "Circuito aperto EV 2" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 6, Meaning = "Sovracorrente EV 3" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 7, Meaning = "Circuito aperto EV 3" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 8, Meaning = "Sovracorrente EV 4" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 9, Meaning = "Circuito aperto EV 4" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 10, Meaning = "Sovracorrente EV 5" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 11, Meaning = "Circuito aperto EV 5" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 12, Meaning = "Sovracorrente EV 6" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 13, Meaning = "Circuito aperto EV 6" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 14, Meaning = "Sovracorrente EV 7" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 15, Meaning = "Circuito aperto EV 7" },

            // Word 1: Allarmi (continua)
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 0, Meaning = "Sovracorrente EV 8" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 1, Meaning = "Circuito aperto EV 8" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 2, Meaning = "Low battery" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 3, Meaning = "Costa sensibile" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 4, Meaning = "Errore interno routine software" },
            new() { VariableId = allarmi.Id, DictionaryId = dictId,
                WordIndex = 1, BitIndex = 5, Meaning = "Errore hardware EEPROM esterna" },
        };
        context.Set<BitInterpretationEntity>().AddRange(allarmiBits);

        // === BitInterpretation per-dizionario per Stato ingressi fisici (0x15) ===
        var ingressi = standardVariables.First(v => v.AddressLow == 0x15);
        var ingressiBits = new BitInterpretationEntity[]
        {
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "FC Estratto" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 1, Meaning = "FC Chiuso" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 2, Meaning = "Comando UP" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 3, Meaning = "Comando Down" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 4, Meaning = "Costa sensibile" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 5, Meaning = "Comando tastiera testa su" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 6, Meaning = "Comando tastiera piedi su" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 7, Meaning = "Comando tastiera vai orizzontale" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 8, Meaning = "Comando tastiera molleggio" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 9, Meaning = "Comando tastiera tutto su" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 10, Meaning = "Comando tastiera tutto giù" },
            new() { VariableId = ingressi.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 11, Meaning = "Comando tastiera stop" },
        };
        context.Set<BitInterpretationEntity>().AddRange(ingressiBits);

        // === BitInterpretation per-dizionario per Stato uscite fisiche (0x16) ===
        var uscite = standardVariables.First(v => v.AddressLow == 0x16);
        var usciteBits = new BitInterpretationEntity[]
        {
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 0, Meaning = "EV1" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 1, Meaning = "EV2" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 2, Meaning = "EV3" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 3, Meaning = "EV4" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 4, Meaning = "EV5" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 5, Meaning = "EV6" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 6, Meaning = "EV7" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 7, Meaning = "EV8" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 8, Meaning = "PUMP" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 9, Meaning = "LEDB" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 10, Meaning = "LEDG" },
            new() { VariableId = uscite.Id, DictionaryId = dictId,
                WordIndex = 0, BitIndex = 11, Meaning = "LEDR" },
        };
        context.Set<BitInterpretationEntity>().AddRange(usciteBits);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// BitInterpretation Word 3 (BYTE 0, big-endian) per LED (Verde/Rosso).
    /// bit 0: fisso, bit 1: lampeggiante, bit 2: single shot/loop, bit 3-7: ripetizioni per ciclo
    /// </summary>
    private static BitInterpretationEntity[] LedBits(int variableId, int wordIndex) =>
    [
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 0, Meaning = "Led acceso fisso" },
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 1, Meaning = "Led lampeggiante" },
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 2, Meaning = "Single shot (1) / Loop (0)" },
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 3, Meaning = "Bit dal 3 al 7 (inclusi) rappresentano un numero intero, il numero di lampeggi per ciclo" },
    ];

    /// <summary>
    /// BitInterpretation Word 3 (BYTE 0, big-endian) per Buzzer.
    /// bit 0: fisso, bit 1: lampeggiante, bit 2-7: ripetizioni per ciclo
    /// </summary>
    private static BitInterpretationEntity[] BuzzerBits(int variableId, int wordIndex) =>
    [
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 0, Meaning = "Buzzer acceso fisso" },
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 1, Meaning = "Buzzer lampeggiante" },
        new() { VariableId = variableId, WordIndex = wordIndex, BitIndex = 2, Meaning = "Bit dal 2 al 7 (inclusi) rappresentano un numero intero, il numero di lampeggi per ciclo" },
    ];

    /// <summary>
    /// BitInterpretation per le Word di timing (Word 0/1/2, big-endian).
    /// Ogni word è un valore intero in unità di 4 ms.
    /// </summary>
    private static BitInterpretationEntity[] TimingWordBits(int variableId) =>
    [
        new() { VariableId = variableId, WordIndex = 0, BitIndex = 0, Meaning = "Questa word rappresenta il tempo di pausa tra i cicli di lampeggi in unità di 4 ms" },
        new() { VariableId = variableId, WordIndex = 1, BitIndex = 0, Meaning = "Questa word rappresenta il tempo di OFF tra i singoli lampeggi in unità di 4 ms" },
        new() { VariableId = variableId, WordIndex = 2, BitIndex = 0, Meaning = "Questa word rappresenta il tempo di ON tra i singoli lampeggi in unità di 4 ms" },
    ];

    /// <summary>
    /// Helper per creare una VariableEntity standard (AddressHigh = 0x00).
    /// </summary>
    private static VariableEntity Var(int dictionaryId, string name, byte addressLow,
        DataTypeKind dataTypeKind, string dataTypeRaw, AccessMode accessMode,
        int? dataTypeParam = null, string? format = null,
        double? min = null, double? max = null, string? unit = null,
        string? description = null, bool isEnabled = true, int? wordSize = null)
    {
        return Var(dictionaryId, name, 0x00, addressLow,
            dataTypeKind, dataTypeRaw, accessMode,
            dataTypeParam, format, min, max, unit, description, isEnabled, wordSize);
    }

    /// <summary>
    /// Helper per creare una VariableEntity con AddressHigh esplicito.
    /// </summary>
    private static VariableEntity Var(int dictionaryId, string name, byte addressHigh, byte addressLow,
        DataTypeKind dataTypeKind, string dataTypeRaw, AccessMode accessMode,
        int? dataTypeParam = null, string? format = null,
        double? min = null, double? max = null, string? unit = null,
        string? description = null, bool isEnabled = true, int? wordSize = null)
    {
        return new VariableEntity
        {
            DictionaryId = dictionaryId,
            Name = name,
            AddressHigh = addressHigh,
            AddressLow = addressLow,
            DataTypeKind = dataTypeKind,
            DataTypeRaw = dataTypeRaw,
            DataTypeParam = dataTypeParam,
            AccessMode = accessMode,
            Format = format,
            MinValue = min,
            MaxValue = max,
            Unit = unit,
            Description = description,
            IsEnabled = isEnabled,
            WordSize = wordSize
        };
    }

    /// <summary>
    /// Helper per creare un CommandEntity con parametri.
    /// Formato parametro: "size|description" (es. "1|Stato", "2|IndirizzoHL")
    /// </summary>
    private static CommandEntity Cmd(string name, byte codeHigh, byte codeLow, bool isResponse,
        params string[] parameters)
    {
        // Serializza i parametri come JSON array
        var paramsJson = parameters.Length > 0
            ? "[" + string.Join(",", parameters.Select(p => $"\"{p}\"")) + "]"
            : "[]";

        return new CommandEntity
        {
            Name = name,
            CodeHigh = codeHigh,
            CodeLow = codeLow,
            IsResponse = isResponse,
            ParametersJson = paramsJson
        };
    }

    /// <summary>
    /// Helper per creare una BoardEntity con indirizzo calcolato.
    /// Formula: (machineCode &lt;&lt; 16) | ((fwType &amp; 0x3FF) &lt;&lt; 6) | (boardNumber &amp; 0x3F)
    /// </summary>
    private static BoardEntity Brd(int machineCode, int deviceId, string name,
        int fwType, int boardNumber, bool isPrimary)
    {
        var address = (uint)((machineCode << 16)
            | ((fwType & 0x3FF) << 6)
            | (boardNumber & 0x3F));

        return new BoardEntity
        {
            DeviceId = deviceId,
            Name = name,
            FirmwareType = fwType,
            BoardNumber = boardNumber,
            IsPrimary = isPrimary,
            ProtocolAddress = address,
        };
    }
}
