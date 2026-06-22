using System.Text.Json;
using Core.Enums;
using Core.Models;
using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Mapping;

namespace Services;

/// <summary>
/// Board service implementation.
/// SESSION_035: DeviceType enum → DeviceId FK.
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly IAuditService _audit;
    private readonly ICurrentUserProvider _userProvider;
    private readonly ILogger<BoardService> _logger;

    public BoardService(
        IBoardRepository boardRepository,
        IDictionaryRepository dictionaryRepository,
        IAuditService auditService,
        ICurrentUserProvider userProvider,
        ILogger<BoardService> logger)
    {
        ArgumentNullException.ThrowIfNull(boardRepository);
        ArgumentNullException.ThrowIfNull(dictionaryRepository);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(userProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _boardRepository = boardRepository;
        _dictionaryRepository = dictionaryRepository;
        _audit = auditService;
        _userProvider = userProvider;
        _logger = logger;
    }

    public async Task<Board?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        BoardEntity? entity = await _boardRepository.GetByIdAsync(id, ct);
        return entity is null ? null : BoardMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Board>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BoardEntity> entities = await _boardRepository.GetAllAsync(ct);
        return BoardMapper.ToDomainList(entities);
    }

    public async Task<Board> AddAsync(Board board, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        // Check that the dictionary exists (if specified)
        if (board.DictionaryId.HasValue)
        {
            DictionaryEntity dict = await _dictionaryRepository.GetByIdAsync(board.DictionaryId.Value, ct) ?? throw new InvalidOperationException(
                    $"Dictionary (Id={board.DictionaryId.Value}) not found.");
        }
        else
        {
            // Auto-assign: if other boards with the same FirmwareType have a dictionary,
            // inherit it automatically (e.g. shared Pulsantiere for FW=4).
            IReadOnlyList<BoardEntity> allBoards = await _boardRepository.GetAllAsync(ct);
            int sharedDictId = allBoards
                .Where(b => b.FirmwareType == board.FirmwareType && b.DictionaryId.HasValue)
                .Select(b => b.DictionaryId!.Value)
                .Distinct()
                .FirstOrDefault();

            if (sharedDictId > 0)
            {
                board = new Board(
                    board.DeviceId, board.Name, board.FirmwareType, board.BoardNumber,
                    board.MachineCode, board.PartNumber, board.IsPrimary,
                    dictionaryId: sharedDictId);
            }
        }

        // Validation: max 1 IsPrimary per Device (BR-005)
        if (board.IsPrimary)
        {
            await EnsureNoPrimaryExistsAsync(board.DeviceId, excludeBoardId: null, ct);
        }

        BoardEntity entity = BoardMapper.ToEntity(board);
        BoardEntity created = await _boardRepository.AddAsync(entity, ct);

        BoardEntity? result = await _boardRepository.GetByIdAsync(created.Id, ct);
        Board domain = BoardMapper.ToDomain(result!);

        _logger.LogInformation(
            "Created board {BoardId} ({Name}) for device {DeviceId}",
            domain.Id, domain.Name, domain.DeviceId);

        await _audit.LogCreateAsync(AuditEntityType.Board, domain.Id,
            _userProvider.CurrentUserId ?? 0,
            JsonSerializer.Serialize(domain), ct: ct);

        return domain;
    }

    public async Task UpdateAsync(Board board, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        BoardEntity entity = await _boardRepository.GetByIdAsync(board.Id, ct)
            ?? throw new KeyNotFoundException(
                $"Board '{board.Name}' (Id={board.Id}) not found.");

        // Check dictionary if specified
        if (board.DictionaryId.HasValue)
        {
            _ = await _dictionaryRepository.GetByIdAsync(board.DictionaryId.Value, ct) ?? throw new InvalidOperationException(
                    $"Dictionary (Id={board.DictionaryId.Value}) not found.");
        }

        // Validation: max 1 IsPrimary per Device (BR-005)
        if (board.IsPrimary)
        {
            await EnsureNoPrimaryExistsAsync(board.DeviceId, excludeBoardId: board.Id, ct);
        }

        Board previous = BoardMapper.ToDomain(entity);
        string prevJson = JsonSerializer.Serialize(previous);

        BoardMapper.UpdateEntity(entity, board);
        await _boardRepository.UpdateAsync(entity, ct);

        await _audit.LogUpdateAsync(AuditEntityType.Board, board.Id,
            _userProvider.CurrentUserId ?? 0,
            prevJson, JsonSerializer.Serialize(board), ct: ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        BoardEntity? board = await _boardRepository.GetByIdAsync(id, ct);
        string? previousJson = board is not null
            ? JsonSerializer.Serialize(BoardMapper.ToDomain(board))
            : null;

        if (board?.DictionaryId is int dictId)
        {
            // If the dictionary is only referenced by this board, delete it
            IReadOnlyList<BoardEntity> allBoards = await _boardRepository.GetAllAsync(ct);
            int refCount = allBoards.Count(b => b.DictionaryId == dictId);
            if (refCount <= 1)
            {
                // Delete the board first (FK), then the dictionary
                await _boardRepository.DeleteAsync(id, ct);
                await _dictionaryRepository.DeleteAsync(dictId, ct);

                if (previousJson is not null)
                {
                    await _audit.LogDeleteAsync(AuditEntityType.Board, id,
                        _userProvider.CurrentUserId ?? 0, previousJson, ct: ct);
                }

                return;
            }
        }

        await _boardRepository.DeleteAsync(id, ct);

        if (previousJson is not null)
        {
            await _audit.LogDeleteAsync(AuditEntityType.Board, id,
                _userProvider.CurrentUserId ?? 0, previousJson, ct: ct);
        }
    }

    public async Task<IReadOnlyList<Board>> GetByDeviceIdAsync(int deviceId,
        CancellationToken ct = default)
    {
        IReadOnlyList<BoardEntity> entities = await _boardRepository.GetByDeviceIdAsync(deviceId, ct);
        return BoardMapper.ToDomainList(entities);
    }

    public async Task<Board?> GetByProtocolAddressAsync(uint protocolAddress,
        CancellationToken ct = default)
    {
        BoardEntity? entity = await _boardRepository.GetByProtocolAddressAsync(protocolAddress, ct);
        return entity is null ? null : BoardMapper.ToDomain(entity);
    }

    public async Task<int> GetNextAvailableFirmwareTypeAsync(CancellationToken ct = default)
    {
        IReadOnlyList<BoardEntity> all = await _boardRepository.GetAllAsync(ct);
        int maxFw = all.Count > 0 ? all.Max(b => b.FirmwareType) : 0;
        return maxFw + 1;
    }

    // === Private helpers ===

    private async Task EnsureNoPrimaryExistsAsync(
        int deviceId, int? excludeBoardId, CancellationToken ct)
    {
        IReadOnlyList<BoardEntity> boards = await _boardRepository.GetByDeviceIdAsync(deviceId, ct);
        BoardEntity? existing = boards.FirstOrDefault(b =>
            b.IsPrimary && b.Id != (excludeBoardId ?? -1));

        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"This device already has a primary board ('{existing.Name}').");
        }
    }
}
