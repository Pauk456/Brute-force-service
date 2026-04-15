namespace Worker.Infrastructure.Options;

public sealed class WorkerOptions
{
    public const string SectionName = "Worker";

    public string ManagerCallbackUrl { get; init; } = "http://manager:8080/internal/api/manager/hash/crack/request";
}
