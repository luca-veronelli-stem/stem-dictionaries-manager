using Core.Enums;
using Core.Models;
using Infrastructure.Interfaces;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Implementazione service per gestione schede e tipi scheda.
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardTypeRepository _boardTypeRepository;

    public BoardService(IBoardRepository boardRepository, IBoardTypeRepository boardTypeRepository)
    {
        ArgumentNullException.ThrowIfNull(boardRepository);
        ArgumentNullException.ThrowIfNull(boardTypeRepository);
        _boardRepository = boardRepository;
        _boardTypeRepository = boardTypeRepository;
    }

    // === Board CRUD ===

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
        
        // Verifica che BoardType esista
        var boardType = await _boardTypeRepository.GetByIdAsync(board.BoardType.Id, ct)
            ?? throw new InvalidOperationException($"BoardType with Id {board.BoardType.Id} not found.");
        
        var entity = BoardMapper.ToEntity(board);
        var created = await _boardRepository.AddAsync(entity, ct);
        
        // Ricarica con BoardType
        var result = await _boardRepository.GetByIdAsync(created.Id, ct);
        return BoardMapper.ToDomain(result!);
    }

    public async Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(board);
        
        var entity = await _boardRepository.GetByIdAsync(board.Id, ct)
            ?? throw new KeyNotFoundException($"Board with Id {board.Id} not found.");
        
        BoardMapper.UpdateEntity(entity, board);
        await _boardRepository.UpdateAsync(entity, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await _boardRepository.DeleteAsync(id, ct);
    }

    // === Board Query ===

    public async Task<IReadOnlyList<Board>> GetByDeviceTypeAsync(DeviceType deviceType, CancellationToken ct = default)
    {
        var entities = await _boardRepository.GetByDeviceTypeAsync(deviceType, ct);
        return BoardMapper.ToDomainList(entities);
    }

    public async Task<Board?> GetByProtocolAddressAsync(uint protocolAddress, CancellationToken ct = default)
    {
        var entity = await _boardRepository.GetByProtocolAddressAsync(protocolAddress, ct);
        return entity is null ? null : BoardMapper.ToDomain(entity);
    }

    // === BoardType Operations ===

    public async Task<IReadOnlyList<BoardType>> GetBoardTypesAsync(CancellationToken ct = default)
    {
        var entities = await _boardTypeRepository.GetAllAsync(ct);
        return BoardTypeMapper.ToDomainList(entities);
    }

    public async Task<BoardType?> GetBoardTypeByNameAsync(string name, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var entity = await _boardTypeRepository.GetByNameAsync(name, ct);
        return entity is null ? null : BoardTypeMapper.ToDomain(entity);
    }

    public async Task<BoardType?> GetBoardTypeByFirmwareTypeAsync(int firmwareType, CancellationToken ct = default)
    {
        var entity = await _boardTypeRepository.GetByFirmwareTypeAsync(firmwareType, ct);
        return entity is null ? null : BoardTypeMapper.ToDomain(entity);
    }

    public async Task<BoardType> AddBoardTypeAsync(BoardType boardType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(boardType);
        
        // Verifica unicità nome
        var existingByName = await _boardTypeRepository.GetByNameAsync(boardType.Name, ct);
        if (existingByName is not null)
            throw new InvalidOperationException($"BoardType with name '{boardType.Name}' already exists.");
        
        // Verifica unicità firmwareType
        var existingByFw = await _boardTypeRepository.GetByFirmwareTypeAsync(boardType.FirmwareType, ct);
        if (existingByFw is not null)
            throw new InvalidOperationException($"BoardType with firmware type {boardType.FirmwareType} already exists.");
        
        var entity = BoardTypeMapper.ToEntity(boardType);
        var created = await _boardTypeRepository.AddAsync(entity, ct);
        return BoardTypeMapper.ToDomain(created);
    }
}
