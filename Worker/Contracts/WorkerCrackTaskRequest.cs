namespace Worker.Contracts;

public sealed class WorkerCrackTaskRequest
{
    public required string RequestId { get; init; }
    public required string Hash { get; init; }
    public required string Alphabet { get; init; }
    public required int MaxLength { get; init; }
    public required int PartNumber { get; init; }
    public required int PartCount { get; init; }
}
