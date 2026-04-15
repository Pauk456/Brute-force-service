using System.Collections.Concurrent;
using Manager.Domain.Entities;

namespace Manager.Infrastructure.Repositories;

public sealed class InMemoryCrackRequestRepository
{
    private readonly ConcurrentDictionary<Guid, CrackRequestState> _requests = new();

    public CrackRequestState Add(CrackRequestState state)
    {
        _requests[state.RequestId] = state;
        return state;
    }

    public bool TryGet(Guid requestId, out CrackRequestState? state) => _requests.TryGetValue(requestId, out state);

    public IEnumerable<CrackRequestState> GetAllInProgress() => _requests.Values.Where(static x => x.Status == Domain.Enums.RequestStatus.InProgress);
}
