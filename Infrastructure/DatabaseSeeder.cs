using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

/// <summary>
/// Popola il database con dati iniziali.
/// Utenti + Comandi: il resto viene inserito manualmente dalla GUI.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Se esistono già utenti, non fare nulla
        if (await context.Users.AnyAsync())
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

        await context.SaveChangesAsync();
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
}
