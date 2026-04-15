namespace Manager.Infrastructure.Options;

public sealed class ManagerOptions
{
    public const string SectionName = "Manager";

    public int WorkerCount { get; init; } = 1;
    public int RequestTimeoutSeconds { get; init; } = 120;
    public string WorkerBaseUrl { get; init; } = "http://worker:8080";
    public string Alphabet { get; init; } = "abcdefghijklmnopqrstuvwxyz0123456789";
}
