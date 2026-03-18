# Stem.Dictionaries.Manager - Issue Tracker

> **Ultimo aggiornamento:** 2026-03-18

---

## Riepilogo Globale

| Componente | Aperte | Risolte | Totale |
|------------|--------|---------|--------|
| [Core](./Core/ISSUES.md) | 5 | 0 | 5 |
| [Infrastructure](./Infrastructure/ISSUES.md) | 5 | 1 | 6 |
| [Services](./Services/ISSUES.md) | 7 | 0 | 7 |
| GUI.Windows | - | - | - |
| [Tests](./Tests/ISSUES.md) | 3 | 3 | 6 |
| **Totale** | **20** | **4** | **24** |

---

## Distribuzione per Priorità

| Priorità | Aperte | % |
|----------|--------|---|
| **Critica** | 0 | 0% |
| **Alta** | 0 | 0% |
| **Media** | 7 | 35% |
| **Bassa** | 13 | 65% |
| **Totale** | **20** | 100% |

```
Critica:     ░░░░░░░░░░░░░░░░░░░░  0
Alta:        ░░░░░░░░░░░░░░░░░░░░  0  ✅ Risolte tutte
Media:       ███████░░░░░░░░░░░░░  7
Bassa:       █████████████░░░░░░░ 13
```

---

## Issue Alta Priorità

| ID | Componente | Titolo | Status |
|----|------------|--------|--------|
| ~~INFRA-001~~ | Infrastructure | DeleteAsync non solleva eccezione | ✅ **Risolto** |

*Nessuna issue alta priorità aperta.*

---

## Issue Trasversali (T-xxx)

> **Nota:** Le issue trasversali saranno aggiunte quando GUI.Windows sara implementato.
> Per ora, le issue sono isolate per componente.

| ID | Titolo | Status | Componenti Coinvolti |
|----|--------|--------|----------------------|
| - | *Nessuna issue trasversale identificata* | - | - |

---

## Issue per Componente

### Core (5 issue)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| [CORE-001](./Core/ISSUES.md#core-001--auditentitytype-contiene-device-non-esistente-nel-dominio) | AuditEntityType contiene "Device" non esistente | Media | Design |
| [CORE-002](./Core/ISSUES.md#core-002--variablecategory-deriva-solo-da-addresshigh--0x00) | Variable.Category deriva solo da AddressHigh == 0x00 | Media | Design |
| [CORE-003](./Core/ISSUES.md#core-003--dictionaryremovevariable-non-verifica-esistenza) | Dictionary.RemoveVariable non verifica esistenza | Bassa | API |
| [CORE-004](./Core/ISSUES.md#core-004--mancanza-di-metodi-update-sui-modelli) | Mancanza di metodi Update sui modelli | Bassa | API |
| [CORE-005](./Core/ISSUES.md#core-005--bitinterpretationvariableid-non-ha-validazione-positiva) | BitInterpretation.VariableId non ha validazione | Bassa | API |

### Infrastructure (6 issue)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~INFRA-001~~ | ~~DeleteAsync non solleva eccezione~~ | ~~Alta~~ | ✅ **Risolto** |
| [INFRA-002](./Infrastructure/ISSUES.md#infra-002--getallasync-senza-paginazione-rischia-performance-issues) | GetAllAsync senza paginazione | Media | Performance |
| [INFRA-003](./Infrastructure/ISSUES.md#infra-003--designtimedbcontextfactory-ha-path-hardcoded-fragile) | DesignTimeDbContextFactory path fragile | Media | Manutenibilità |
| [INFRA-004](./Infrastructure/ISSUES.md#infra-004--mancano-repository-per-bitinterpretation-e-commanddevicestate) | Mancano repository BitInterpretation/CommandDeviceState | Bassa | API |
| [INFRA-005](./Infrastructure/ISSUES.md#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata) | ParametersJson stringa grezza | Bassa | Design |
| [INFRA-006](./Infrastructure/ISSUES.md#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input) | GetByNameAsync non normalizza input | Bassa | Bug |

### Tests (3 issue aperte, 3 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~TEST-001~~ | ~~Mancano test BoardRepository e CommandRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-002~~ | ~~Mancano test BoardTypeRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-003~~ | ~~Uso .Wait() bloccante~~ | ~~Media~~ | ✅ **Risolto** |
| [TEST-004](./Tests/ISSUES.md#test-004--mancano-test-per-dependencyinjection-infrastructure-e-services) | Mancano test DI | Bassa | Struttura |
| [TEST-005](./Tests/ISSUES.md#test-005--mancano-test-per-scenari-di-rilavorazioneupdate-entities) | Mancano test scenari update/delete | Bassa | Copertura |
| [TEST-006](./Tests/ISSUES.md#test-006--magic-strings-ripetute-nei-test) | Magic strings ripetute | Bassa | Manutenibilità |

### Services (7 issue aperte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| [SVC-001](./Services/ISSUES.md#svc-001--services-dipendono-direttamente-da-appdbcontext) | Services dipendono da AppDbContext | Media | Design |
| [SVC-002](./Services/ISSUES.md#svc-002--manca-iauditservice-per-gestione-audit-trail) | Manca IAuditService | Media | Feature |
| [SVC-003](./Services/ISSUES.md#svc-003--getallasync-senza-paginazione-nei-services) | GetAllAsync senza paginazione | Media | Performance |
| [SVC-004](./Services/ISSUES.md#svc-004--mancano-mapper-per-boardmapper-con-overload) | BoardMapper overload mancanti | Bassa | Code Smell |
| [SVC-005](./Services/ISSUES.md#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates) | GetWithDeviceStates non espone stati | Bassa | Design |
| [SVC-006](./Services/ISSUES.md#svc-006--manca-validazione-business-rules-centralizzata) | Manca validazione centralizzata | Bassa | Design |
| [SVC-007](./Services/ISSUES.md#svc-007--dependencyinjection-non-valida-prerequisiti) | DI non valida prerequisiti | Bassa | Robustezza |

### GUI.Windows (da sviluppare)

*Il layer GUI.Windows non è ancora implementato. Le issue saranno tracciate quando verrà sviluppato.*

---

## Top 5 Issue da Risolvere

| # | ID | Componente | Titolo | Effort |
|---|-----|------------|--------|--------|
| 1 | **SVC-001** | Services | Services dipendono da AppDbContext | M |
| 2 | **CORE-001** | Core | AuditEntityType contiene "Device" | S |
| 3 | **SVC-003** | Services | GetAllAsync senza paginazione | M |
| 4 | **INFRA-002** | Infrastructure | GetAllAsync senza paginazione | M |
| 5 | **CORE-002** | Core | Variable.Category deriva solo da AddressHigh | S |

**Effort:** S = 1-2h, M = 4-8h, L = 1-2 giorni

---

## Copertura Test Attuale

| Componente | Unit | Integration | Copertura |
|------------|------|-------------|-----------|
| Core/Enums (6) | ✅ 25 | - | 100% |
| Core/Models (9) | ✅ 97 | - | 100% |
| Infrastructure/Repositories (7) | - | ✅ 57 | ~95% |
| Services/Mapping (8) | ✅ 60 | - | ~90% |
| Services (5) | - | ✅ 65 | ~80% |
| GUI.Windows | - | - | N/A |

**Totale test:** 304 (182 unit + 122 integration)

---

## Metriche Qualità

| Aspetto | Stato | Note |
|---------|-------|------|
| **Architecture** | ✅ 90% | Layer separation corretta |
| **Thread Safety** | ✅ 95% | Modelli immutabili |
| **Input Validation** | ⚠️ 75% | Alcuni edge-case (CORE-002, CORE-005) |
| **Performance** | ⚠️ 70% | GetAllAsync senza paginazione (INFRA-002, SVC-003) |
| **Code Consistency** | ✅ 85% | Poche inconsistenze (INFRA-006) |
| **Test Coverage** | ⚠️ 70% | Repository non tutti testati |

---

## Issue per Categoria

| Categoria | Count | Issue |
|-----------|-------|-------|
| **API** | 5 | CORE-003, CORE-004, CORE-005, INFRA-001, INFRA-004 |
| **Design** | 3 | CORE-001, CORE-002, INFRA-005 |
| **Copertura** | 3 | TEST-001, TEST-002, TEST-005 |
| **Performance** | 1 | INFRA-002 |
| **Manutenibilità** | 2 | INFRA-003, TEST-006 |
| **Anti-Pattern** | 1 | TEST-003 |
| **Struttura** | 1 | TEST-004 |
| **Bug** | 1 | INFRA-006 |

---

## Come Contribuire

1. Seleziona issue da priorità alta o Top 5
2. Crea branch: `fix/{issue-id}` (es. `fix/infra-001`)
3. Implementa soluzione proposta nel file ISSUES.md del componente
4. Aggiungi test se applicabile
5. Aggiorna status issue a "Risolto" con data e branch
6. Pull Request verso `main`

---

## Links

- [ISSUES_TEMPLATE.md](./Docs/Standards/Templates/ISSUES_TEMPLATE.md) - Template per nuove issue
- [copilot-instructions.md](.copilot/copilot-instructions.md) - Istruzioni progetto
- [issues-agent.md](.copilot/agents/issues-agent.md) - Agent per analisi issue

---

## Changelog

| Data | Modifica |
|------|----------|
| 2026-03-18 | ✅ **TEST-001/002 risolte** - Aggiunti 33 test per BoardRepository, BoardTypeRepository, CommandRepository (branch `fix/test-001-002`) |
| 2026-03-18 | ✅ **INFRA-001 risolta** - DeleteAsync ora lancia KeyNotFoundException (branch `fix/infra-001`) |
| 2026-03-18 | Creazione iniziale con 17 issue da 3 componenti (Core, Infrastructure, Tests) |
