using System.Collections.Concurrent;
using Manager.Domain.Enums;

namespace Manager.Domain.Entities;

public sealed class CrackRequestState
{
    public CrackRequestState(Guid requestId, string hash, DateTime createdAtUtc)
    {
        RequestId = requestId;
        Hash = hash;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RequestId { get; }
    public string Hash { get; }
    public DateTime CreatedAtUtc { get; }
    public int ExpectedParts { get; private set; }
    public RequestStatus Status { get; private set; } = RequestStatus.InProgress;
    public ConcurrentDictionary<int, IReadOnlyCollection<string>> CompletedParts { get; } = new();
    public IReadOnlyCollection<string>? ResultWords { get; private set; }
    public bool IsQueued { get; private set; } = true;

    public bool TryStart(int expectedParts)
    {
        if (Status != RequestStatus.InProgress || !IsQueued || expectedParts <= 0)
        {
            return false;
        }

        ExpectedParts = expectedParts;
        IsQueued = false;
        return true;
    }

    public bool TrySetReadyFromCache(IReadOnlyCollection<string> words)
    {
        if (Status != RequestStatus.InProgress)
        {
            return false;
        }

        IsQueued = false;
        ExpectedParts = 0;
        ResultWords = words.ToArray();
        Status = RequestStatus.Ready;
        return true;
    }

    public bool TryAddPartResult(int partNumber, IReadOnlyCollection<string> words)
    {
        if ((Status != RequestStatus.InProgress && Status != RequestStatus.PartialReady) || IsQueued || ExpectedParts <= 0)
        {
            return false;
        }

        var added = CompletedParts.TryAdd(partNumber, words);
        if (!added)
        {
            return false;
        }

        ResultWords = CompletedParts.Values.SelectMany(static x => x).Distinct().ToArray();

        if (CompletedParts.Count < ExpectedParts)
        {
            Status = RequestStatus.PartialReady;
            return true;
        }

        Status = RequestStatus.Ready;
        return true;
    }

    public void MarkAsError()
    {
        if (Status == RequestStatus.InProgress)
        {
            Status = RequestStatus.Error;
        }
    }
}
