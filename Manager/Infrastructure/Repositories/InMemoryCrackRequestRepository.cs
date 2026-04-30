using System.Collections.Concurrent;
using Manager.Domain.Entities;
using Manager.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Manager.Infrastructure.Repositories;

public sealed class InMemoryCrackRequestRepository
{
    private readonly ConcurrentDictionary<Guid, CrackRequestState> _requests = new();
    private readonly ConcurrentDictionary<string, IReadOnlyCollection<string>> _completedHashes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Guid> _completedHashRequestIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _completedHashInsertionOrder = new();
    private readonly int _maxCompletedHashCacheEntries;

    public InMemoryCrackRequestRepository(IOptions<ManagerOptions> options)
    {
        _maxCompletedHashCacheEntries = Math.Max(0, options.Value.MaxCompletedHashCacheEntries);
    }

    public CrackRequestState Add(CrackRequestState state)
    {
        _requests[state.RequestId] = state;
        return state;
    }

    public bool TryRemove(Guid requestId) => _requests.TryRemove(requestId, out _);

    public bool TryGet(Guid requestId, out CrackRequestState? state) => _requests.TryGetValue(requestId, out state);

    public IEnumerable<CrackRequestState> GetAllInProgress() => _requests.Values.Where(static x => x.Status == Domain.Enums.RequestStatus.InProgress);

    public bool TryGetInProgressByHash(string hash, out CrackRequestState? state)
    {
        state = _requests.Values.FirstOrDefault(x =>
            (x.Status == Domain.Enums.RequestStatus.InProgress || x.Status == Domain.Enums.RequestStatus.PartialReady) &&
            string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
        return state is not null;
    }

    public bool TryGetCompletedHash(string hash, out Guid requestId, out IReadOnlyCollection<string>? resultWords)
    {
        requestId = Guid.Empty;
        resultWords = null;

        if (!_completedHashes.TryGetValue(hash, out resultWords))
        {
            return false;
        }

        if (!_completedHashRequestIds.TryGetValue(hash, out requestId))
        {
            resultWords = null;
            return false;
        }

        return true;
    }

    public void TryStoreCompletedHash(string hash, Guid requestId, IReadOnlyCollection<string> resultWords)
    {
        if (_maxCompletedHashCacheEntries <= 0)
        {
            return;
        }

        if (_completedHashes.TryAdd(hash, resultWords))
        {
            _completedHashRequestIds[hash] = requestId;
            _completedHashInsertionOrder.Enqueue(hash);
            TrimCompletedHashCache();
        }
    }

    private void TrimCompletedHashCache()
    {
        while (_completedHashes.Count > _maxCompletedHashCacheEntries &&
               _completedHashInsertionOrder.TryDequeue(out var oldestHash))
        {
            _completedHashes.TryRemove(oldestHash, out _);
            _completedHashRequestIds.TryRemove(oldestHash, out _);
        }
    }

    public IReadOnlyCollection<CrackRequestState> GetInProgressSnapshot() =>
        _requests.Values
            .Where(static x => x.Status is Domain.Enums.RequestStatus.InProgress or Domain.Enums.RequestStatus.PartialReady)
            .ToArray();

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> GetCompletedHashesSnapshot() =>
        _completedHashes.ToDictionary(
            static x => x.Key,
            static x => x.Value,
            StringComparer.OrdinalIgnoreCase);
}
