using Manager.Application.Services;

namespace Manager.Infrastructure.BackgroundServices;

public sealed class RequestTimeoutBackgroundService : BackgroundService
{
    private readonly HashCrackService _hashCrackService;

    public RequestTimeoutBackgroundService(HashCrackService hashCrackService)
    {
        _hashCrackService = hashCrackService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _hashCrackService.MarkTimedOutRequestsAsError();
        }
    }
}
