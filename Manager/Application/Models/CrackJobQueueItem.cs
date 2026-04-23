namespace Manager.Application.Models;

public sealed class CrackJobQueueItem
{
    public required Guid RequestId { get; init; }
    public required string Hash { get; init; }
    public required int MaxLength { get; init; }
}
