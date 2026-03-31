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
        await SeedStandardDictionaryAsync(context);

        // === Dizionario Pulsantiere (da pulsantiere.CSV) ===
        await SeedPulsantiereDictionaryAsync(context, boards);
    }

    /// <summary>
    /// Crea il dizionario standard con le variabili comuni a tutti i dispositivi STEM.
    /// Fonte: Docs/Dictionaries/dati_standard.CSV
    /// AddressHigh = 0x00 per tutte le variabili standard.
    /// BitInterpretations: vuote nello standard, i device le overridano.
    /// </summary>
    private static async Task SeedStandardDictionaryAsync(AppDbContext context)
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

            // 0x0010 — Salute batteria
            Var(dictionary.Id, "Salute batteria", 0x10,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly,
                description: "Stato di salute (SOH) della batteria"),

            // 0x0011 — Cicli batteria
            Var(dictionary.Id, "Cicli batteria", 0x11,
                DataTypeKind.UInt16, "UInt16",
                AccessMode.ReadOnly,
                description: "Numero di cicli di carica/scarica della batteria"),

            // 0x0012 — Temperatura batteria
            Var(dictionary.Id, "Temperatura batteria", 0x12,
                DataTypeKind.Int16, "Int16",
                AccessMode.ReadOnly,
                description: "Temperatura della batteria in decimi di grado (÷10 per °C)"),

            // 0x0013 — BatteryFirmware
            Var(dictionary.Id, "BatteryFirmware", 0x13,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly,
                description: "Versione firmware della batteria"),

            // 0x0014 — BatterySerial
            Var(dictionary.Id, "BatterySerial", 0x14,
                DataTypeKind.UInt32, "UInt32",
                AccessMode.ReadOnly,
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
    }

    /// <summary>
    /// Crea il dizionario pulsantiere con le variabili specifiche delle tastiere STEM.
    /// Fonte: Docs/Dictionaries/pulsantiere.CSV
    /// AddressHigh = 0x80 per tutte le variabili pulsantiere.
    /// Aggiorna le BoardEntity pulsantiera per puntare a questo dizionario.
    /// </summary>
    private static async Task SeedPulsantiereDictionaryAsync(AppDbContext context, BoardEntity[] boards)
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
