using Core.Models;

namespace Services.Interfaces;

/// <summary>
/// Service per gestione variabili singole.
/// Per operazioni batch, usare IDictionaryService.
/// SESSION_035: DeviceType enum → int deviceId.
/// </summary>
public interface IVariableService
{
    // === CRUD Base ===

    Task<Variable?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Variable>> GetAllAsync(CancellationToken ct = default);
    Task<Variable> AddAsync(int dictionaryId, Variable variable, CancellationToken ct = default);
    Task UpdateAsync(Variable variable, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    // === Query Specifiche ===

    Task<IReadOnlyList<Variable>> GetByDictionaryIdAsync(int dictionaryId, CancellationToken ct = default);

    Task<Variable?> GetByAddressAsync(int dictionaryId, byte addressHigh, byte addressLow,
        CancellationToken ct = default);

    // === BitInterpretation Management ===

    Task<IReadOnlyList<BitInterpretation>> GetBitInterpretationsAsync(int variableId,
        CancellationToken ct = default);

    Task<BitInterpretation> AddBitInterpretationAsync(int variableId, BitInterpretation interpretation,
        CancellationToken ct = default);

    Task UpdateBitInterpretationsAsync(int variableId, IEnumerable<BitInterpretation> interpretations,
        CancellationToken ct = default);

    // === DeviceState Management ===

    Task SetDeviceStateAsync(int variableId, int deviceId, bool isEnabled,
        CancellationToken ct = default);

    Task<VariableDeviceState?> GetDeviceStateAsync(int variableId, int deviceId,
        CancellationToken ct = default);

    Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesAsync(int variableId,
        CancellationToken ct = default);

    Task<IReadOnlyList<VariableDeviceState>> GetDeviceStatesForDeviceAsync(
        int deviceId, CancellationToken ct = default);
}
