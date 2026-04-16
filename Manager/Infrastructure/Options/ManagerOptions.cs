namespace Manager.Infrastructure.Options;

public sealed class ManagerOptions
{
    public const string SectionName = "Manager";

    public int RequestTimeoutSeconds { get; init; } = 120;
    public string[] WorkerBaseUrls { get; init; } = [];
    public string Alphabet { get; init; } = "abcdefghijklmnopqrstuvwxyz0123456789";
}
