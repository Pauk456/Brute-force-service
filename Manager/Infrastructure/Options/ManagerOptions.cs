namespace Manager.Infrastructure.Options;

public sealed class ManagerOptions
{
    public const string SectionName = "Manager";

    public int RequestTimeoutSeconds { get; init; } = 120;
    public int RequestTimeoutCheckIntervalSeconds { get; init; } = 3;
    public int MaxQueuedCrackRequests { get; init; } = 100;
    public int MaxCompletedHashCacheEntries { get; init; } = 1000;
    public string[] WorkerBaseUrls { get; init; } = [];
    public string Alphabet { get; init; } = "abcdefghijklmnopqrstuvwxyz0123456789";
}
