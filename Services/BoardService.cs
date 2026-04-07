using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione schede.
/// SESSION_035: DeviceType enum → DeviceId FK.
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IDictionaryRepository _dictionaryRepository;

    public BoardService(IBoardRepository boardRepository, IDictionaryRepository dictionaryRepository)
    {
        ArgumentNullException.ThrowIfNull(boardRepository);
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        _boardRepository = boardRepository;
        _dictionaryRepository = dictionaryRepository;
    }

    public async Task<Board?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _boardRepository.GetByIdAsync(id, ct);
        return entity is null ? null : BoardMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _boardRepository.GetAllAsync(ct);
        return BoardMapper.ToDomainList(entities);
    }

    public async Task<Board> AddAsync(Board board, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        // Verifica che il dizionario esista (se specificato)
        if (board.DictionaryId.HasValue)
        {
            var dict = await _dictionaryRepository.GetByIdAsync(board.DictionaryId.Value, ct) ?? throw new InvalidOperationException(
                    $"Dictionary (Id={board.DictionaryId.Value}) not found.");
        }
        else
        {
            // Auto-assign: se altre board con lo stesso FirmwareType hanno un dizionario,
            // lo eredita automaticamente (es. Pulsantiere condiviso per FW=4).
            var allBoards = await _boardRepository.GetAllAsync(ct);
            var sharedDictId = allBoards
                .Where(b => b.FirmwareType == board.FirmwareType && b.DictionaryId.HasValue)
                .Select(b => b.DictionaryId!.Value)
                .Distinct()
                .FirstOrDefault();

            if (sharedDictId > 0)
                board = new Board(
                    board.DeviceId, board.Name, board.FirmwareType, board.BoardNumber,
                    board.PartNumber, board.IsPrimary, dictionaryId: sharedDictId,
                    board.MachineCode);
        }

        // Validazione: max 1 IsPrimary per Device (BR-005)
        if (board.IsPrimary)
            await EnsureNoPrimaryExistsAsync(board.DeviceId, excludeBoardId: null, ct);

        var entity = BoardMapper.ToEntity(board);
        var created = await _boardRepository.AddAsync(entity, ct);

        var result = await _boardRepository.GetByIdAsync(created.Id, ct);
        return BoardMapper.ToDomain(result!);
    }

    public async Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        var entity = await _boardRepository.GetByIdAsync(board.Id, ct)
            ?? throw new KeyNotFoundException(
                $"Board '{board.Name}' (Id={board.Id}) not found.");

        // Verifica dizionario se specificato
        if (board.DictionaryId.HasValue)
        {
            _ = await _dictionaryRepository.GetByIdAsync(board.DictionaryId.Value, ct) ?? throw new InvalidOperationException(
                    $"Dictionary (Id={board.DictionaryId.Value}) not found.");
        }

        // Validazione: max 1 IsPrimary per Device (BR-005)
        if (board.IsPrimary)
            await EnsureNoPrimaryExistsAsync(board.DeviceId, excludeBoardId: board.Id, ct);

        BoardMapper.UpdateEntity(entity, board);
        await _boardRepository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var board = await _boardRepository.GetByIdAsync(id, ct);

        if (board?.DictionaryId is int dictId)
        {
            // Se il dizionario è referenziato solo da questa board, eliminalo
            var allBoards = await _boardRepository.GetAllAsync(ct);
            var refCount = allBoards.Count(b => b.DictionaryId == dictId);
            if (refCount <= 1)
            {
                // Elimina prima la board (FK), poi il dizionario
                await _boardRepository.DeleteAsync(id, ct);
                await _dictionaryRepository.DeleteAsync(dictId, ct);
                return;
            }
        }

        await _boardRepository.DeleteAsync(id, ct);
    }

    public async Task<IReadOnlyList<Board>> GetByDeviceIdAsync(int deviceId,
        CancellationToken ct = default)
    {
        var entities = await _boardRepository.GetByDeviceIdAsync(deviceId, ct);
        return BoardMapper.ToDomainList(entities);
    }

    public async Task<Board?> GetByProtocolAddressAsync(uint protocolAddress,
        CancellationToken ct = default)
    {
        var entity = await _boardRepository.GetByProtocolAddressAsync(protocolAddress, ct);
        return entity is null ? null : BoardMapper.ToDomain(entity);
    }

    // === Private helpers ===

    private async Task EnsureNoPrimaryExistsAsync(
        int deviceId, int? excludeBoardId, CancellationToken ct)
    {
        var boards = await _boardRepository.GetByDeviceIdAsync(deviceId, ct);
        var existing = boards.FirstOrDefault(b =>
            b.IsPrimary && b.Id != (excludeBoardId ?? -1));

        if (existing is not null)
            throw new InvalidOperationException(
                $"This device already has a primary board ('{existing.Name}').");
    }
}
