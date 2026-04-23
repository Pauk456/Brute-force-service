using System.Threading.Channels;
using Manager.Application.Models;
using Manager.Application.Services;
using Manager.Infrastructure.Persistence;

namespace Manager.Infrastructure.BackgroundServices;

public sealed class CrackTaskQueueBackgroundService : BackgroundService
{
    private readonly Channel<CrackJobQueueItem> _queue;
    private readonly HashCrackService _service;
    private readonly ManagerStatePersistenceService _statePersistenceService;
    private readonly ILogger<CrackTaskQueueBackgroundService> _logger;

    public CrackTaskQueueBackgroundService(
        Channel<CrackJobQueueItem> queue,
        HashCrackService service,
        ManagerStatePersistenceService statePersistenceService,
        ILogger<CrackTaskQueueBackgroundService> logger)
    {
        _queue = queue;
        _service = service;
        _statePersistenceService = statePersistenceService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var queueItem in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _statePersistenceService.SaveSnapshotAsync(stoppingToken);
                await _service.ProcessQueueItemAsync(queueItem, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process crack queue item for request {RequestId}", queueItem.RequestId);
            }
        }
    }
}
