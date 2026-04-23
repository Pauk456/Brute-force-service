using Manager.Application.Services;
using Manager.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Manager.Infrastructure.BackgroundServices;

public sealed class RequestTimeoutBackgroundService : BackgroundService
{
    private readonly HashCrackService _hashCrackService;
    private readonly ManagerOptions _options;

    public RequestTimeoutBackgroundService(
        HashCrackService hashCrackService,
        IOptions<ManagerOptions> options)
    {
        _hashCrackService = hashCrackService;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _options.RequestTimeoutCheckIntervalSeconds > 0
            ? _options.RequestTimeoutCheckIntervalSeconds
            : 3;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _hashCrackService.MarkTimedOutRequestsAsError();
        }
    }
}
