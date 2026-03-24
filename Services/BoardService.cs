using Core.Enums;
using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione schede.
/// Domain v2: nessun BoardType, FirmwareType diretto su Board.
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
            if (!await _dictionaryRepository.ExistsAsync(board.DictionaryId.Value, ct))
                throw new InvalidOperationException(
                    $"Dictionary with Id {board.DictionaryId.Value} not found.");
        }

        // Validazione: max 1 IsPrimary per DeviceType (BR-005)
        if (board.IsPrimary)
            await EnsureNoPrimaryExistsAsync(board.DeviceType, excludeBoardId: null, ct);

        var entity = BoardMapper.ToEntity(board);
        var created = await _boardRepository.AddAsync(entity, ct);

        var result = await _boardRepository.GetByIdAsync(created.Id, ct);
        return BoardMapper.ToDomain(result!);
    }

    public async Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        var entity = await _boardRepository.GetByIdAsync(board.Id, ct)
            ?? throw new KeyNotFoundException($"Board with Id {board.Id} not found.");

        // Verifica dizionario se specificato
        if (board.DictionaryId.HasValue)
        {
            if (!await _dictionaryRepository.ExistsAsync(board.DictionaryId.Value, ct))
                throw new InvalidOperationException(
                    $"Dictionary with Id {board.DictionaryId.Value} not found.");
        }

        // Validazione: max 1 IsPrimary per DeviceType (BR-005)
        if (board.IsPrimary)
            await EnsureNoPrimaryExistsAsync(board.DeviceType, excludeBoardId: board.Id, ct);

        BoardMapper.UpdateEntity(entity, board);
        await _boardRepository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _boardRepository.DeleteAsync(id, ct);
    }

    public async Task<IReadOnlyList<Board>> GetByDeviceTypeAsync(DeviceType deviceType,
        CancellationToken ct = default)
    {
        var entities = await _boardRepository.GetByDeviceTypeAsync(deviceType, ct);
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
        DeviceType deviceType, int? excludeBoardId, CancellationToken ct)
    {
        var boards = await _boardRepository.GetByDeviceTypeAsync(deviceType, ct);
        var existing = boards.FirstOrDefault(b =>
            b.IsPrimary && b.Id != (excludeBoardId ?? -1));

        if (existing is not null)
            throw new InvalidOperationException(
                $"DeviceType '{deviceType}' already has a primary board (Id={existing.Id}).");
    }
}
