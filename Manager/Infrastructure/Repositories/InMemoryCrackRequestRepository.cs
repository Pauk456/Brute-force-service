using System.Collections.Concurrent;
using Manager.Domain.Entities;

namespace Manager.Infrastructure.Repositories;

public sealed class InMemoryCrackRequestRepository
{
    private readonly ConcurrentDictionary<Guid, CrackRequestState> _requests = new();
    private readonly ConcurrentDictionary<string, IReadOnlyCollection<string>> _completedHashes = new(StringComparer.OrdinalIgnoreCase);

    public CrackRequestState Add(CrackRequestState state)
    {
        _requests[state.RequestId] = state;
        return state;
    }

    public bool TryGet(Guid requestId, out CrackRequestState? state) => _requests.TryGetValue(requestId, out state);

    public IEnumerable<CrackRequestState> GetAllInProgress() => _requests.Values.Where(static x => x.Status == Domain.Enums.RequestStatus.InProgress);

    public bool TryGetInProgressByHash(string hash, out CrackRequestState? state)
    {
        state = _requests.Values.FirstOrDefault(x =>
            x.Status == Domain.Enums.RequestStatus.InProgress &&
            string.Equals(x.Hash, hash, StringComparison.OrdinalIgnoreCase));
        return state is not null;
    }

    public bool TryGetCompletedHash(string hash, out IReadOnlyCollection<string>? resultWords) =>
        _completedHashes.TryGetValue(hash, out resultWords);

    public void TryStoreCompletedHash(string hash, IReadOnlyCollection<string> resultWords)
    {
        _completedHashes.TryAdd(hash, resultWords);
    }
}
