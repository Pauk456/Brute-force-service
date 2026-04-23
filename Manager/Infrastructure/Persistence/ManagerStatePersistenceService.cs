using System.Text.Json;
using Manager.Infrastructure.Repositories;

namespace Manager.Infrastructure.Persistence;

public sealed class ManagerStatePersistenceService
{
    private const string StateFileName = "manager-state.json";
    private readonly InMemoryCrackRequestRepository _repository;
    private readonly ILogger<ManagerStatePersistenceService> _logger;
    private readonly string _stateFilePath;

    public ManagerStatePersistenceService(
        InMemoryCrackRequestRepository repository,
        IWebHostEnvironment environment,
        ILogger<ManagerStatePersistenceService> logger)
    {
        _repository = repository;
        _logger = logger;
        var dataDirectory = Path.Combine(environment.ContentRootPath, "data");
        Directory.CreateDirectory(dataDirectory);
        _stateFilePath = Path.Combine(dataDirectory, StateFileName);
    }

    public async Task SaveSnapshotAsync(CancellationToken cancellationToken)
    {
        var inProgressRequests = _repository.GetInProgressSnapshot()
            .Select(request => new PersistedRequestState
            {
                RequestId = request.RequestId,
                Hash = request.Hash,
                CreatedAtUtc = request.CreatedAtUtc,
                ExpectedParts = request.ExpectedParts,
                IsQueued = request.IsQueued,
                CompletedParts = request.CompletedParts.ToDictionary(
                    static x => x.Key,
                    static x => x.Value.ToArray())
            })
            .ToArray();

        var completedHashes = _repository.GetCompletedHashesSnapshot()
            .ToDictionary(
                static x => x.Key,
                static x => x.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var payload = new PersistedManagerState
        {
            SavedAtUtc = DateTime.UtcNow,
            Requests = inProgressRequests,
            CompletedHashes = completedHashes
        };

        var tempFilePath = _stateFilePath + ".tmp";
        await using (var fileStream = File.Create(tempFilePath))
        {
            await JsonSerializer.SerializeAsync(fileStream, payload, cancellationToken: cancellationToken);
        }

        File.Move(tempFilePath, _stateFilePath, overwrite: true);
        _logger.LogDebug("Manager state snapshot saved to {StateFilePath}", _stateFilePath);
    }

    private sealed class PersistedManagerState
    {
        public DateTime SavedAtUtc { get; init; }
        public required PersistedRequestState[] Requests { get; init; }
        public required Dictionary<string, string[]> CompletedHashes { get; init; }
    }

    private sealed class PersistedRequestState
    {
        public Guid RequestId { get; init; }
        public required string Hash { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public int ExpectedParts { get; init; }
        public bool IsQueued { get; init; }
        public required Dictionary<int, string[]> CompletedParts { get; init; }
    }
}
