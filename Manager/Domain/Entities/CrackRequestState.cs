using System.Collections.Concurrent;
using Manager.Domain.Enums;

namespace Manager.Domain.Entities;

public sealed class CrackRequestState
{
    public CrackRequestState(Guid requestId, DateTime createdAtUtc, int expectedParts)
    {
        RequestId = requestId;
        CreatedAtUtc = createdAtUtc;
        ExpectedParts = expectedParts;
    }

    public Guid RequestId { get; }
    public DateTime CreatedAtUtc { get; }
    public int ExpectedParts { get; }
    public RequestStatus Status { get; private set; } = RequestStatus.InProgress;
    public ConcurrentDictionary<int, IReadOnlyCollection<string>> CompletedParts { get; } = new();
    public IReadOnlyCollection<string>? ResultWords { get; private set; }

    public bool TryAddPartResult(int partNumber, IReadOnlyCollection<string> words)
    {
        if (Status != RequestStatus.InProgress)
        {
            return false;
        }

        var added = CompletedParts.TryAdd(partNumber, words);
        if (!added)
        {
            return false;
        }

        if (CompletedParts.Count < ExpectedParts)
        {
            return true;
        }

        var finalResult = CompletedParts.Values.SelectMany(static x => x).Distinct().ToArray();
        ResultWords = finalResult;
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
