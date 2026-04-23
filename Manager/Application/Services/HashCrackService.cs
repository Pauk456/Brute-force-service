using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using Manager.Application.Models;
using Manager.Contracts;
using Manager.Domain.Entities;
using Manager.Infrastructure.Options;
using Manager.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Manager.Application.Services;

public sealed class HashCrackService
{
    private readonly InMemoryCrackRequestRepository _repository;
    private readonly Channel<CrackJobQueueItem> _queue;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ManagerOptions _options;
    private readonly ILogger<HashCrackService> _logger;

    public HashCrackService(
        InMemoryCrackRequestRepository repository,
        Channel<CrackJobQueueItem> queue,
        IHttpClientFactory httpClientFactory,
        IOptions<ManagerOptions> options,
        ILogger<HashCrackService> logger)
    {
        _repository = repository;
        _queue = queue;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<StartCrackResponseDto> StartCrackAsync(CrackRequestDto request, CancellationToken cancellationToken)
    {
        var normalizedHash = request.Hash.ToLowerInvariant();

        if (_repository.TryGetInProgressByHash(normalizedHash, out var inProgressState) && inProgressState is not null)
        {
            return new StartCrackResponseDto { RequestId = inProgressState.RequestId.ToString() };
        }

        if (_repository.TryGetCompletedHash(normalizedHash, out var cachedWords) && cachedWords is not null)
        {
            var cachedRequestId = Guid.NewGuid();
            var cachedState = new CrackRequestState(cachedRequestId, normalizedHash, DateTime.UtcNow);
            cachedState.TrySetReadyFromCache(cachedWords);
            _repository.Add(cachedState);
            return new StartCrackResponseDto { RequestId = cachedRequestId.ToString() };
        }

        var requestId = Guid.NewGuid();
        var state = new CrackRequestState(requestId, normalizedHash, DateTime.UtcNow);
        _repository.Add(state);
        await _queue.Writer.WriteAsync(
            new CrackJobQueueItem
            {
                RequestId = requestId,
                Hash = normalizedHash,
                MaxLength = request.MaxLength
            },
            cancellationToken);

        return new StartCrackResponseDto { RequestId = requestId.ToString() };
    }

    public CrackStatusResponseDto? GetStatus(Guid requestId)
    {
        if (!_repository.TryGet(requestId, out var state) || state is null)
        {
            return null;
        }

        return new CrackStatusResponseDto
        {
            Status = state.Status switch
            {
                Domain.Enums.RequestStatus.InProgress => "IN_PROGRESS",
                Domain.Enums.RequestStatus.Ready => "READY",
                _ => "ERROR"
            },
            Data = state.Status == Domain.Enums.RequestStatus.Ready ? state.ResultWords : null
        };
    }

    public bool TryApplyWorkerResult(WorkerCrackResultResponse result)
    {
        if (!Guid.TryParse(result.RequestId, out var requestId))
        {
            return false;
        }

        if (!_repository.TryGet(requestId, out var state) || state is null)
        {
            return false;
        }

        var accepted = state.TryAddPartResult(result.PartNumber, result.Words);
        if (accepted && state.Status == Domain.Enums.RequestStatus.Ready && state.ResultWords is not null)
        {
            _repository.TryStoreCompletedHash(state.Hash, state.ResultWords);
        }

        return accepted;
    }

    public void MarkTimedOutRequestsAsError()
    {
        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-_options.RequestTimeoutSeconds);
        foreach (var state in _repository.GetAllInProgress().Where(x => x.CreatedAtUtc <= timeoutThreshold))
        {
            state.MarkAsError();
        }
    }

    public async Task ProcessQueueItemAsync(CrackJobQueueItem queueItem, CancellationToken cancellationToken)
    {
        if (!_repository.TryGet(queueItem.RequestId, out var state) || state is null || state.Status != Domain.Enums.RequestStatus.InProgress)
        {
            return;
        }

        var aliveWorkers = await GetAliveWorkersAsync(cancellationToken);
        if (aliveWorkers.Count == 0)
        {
            _logger.LogWarning("No alive workers for request {RequestId}", queueItem.RequestId);
            state.MarkAsError();
            return;
        }

        if (!state.TryStart(aliveWorkers.Count))
        {
            return;
        }

        var workerTasks = aliveWorkers.Select((workerBaseUrl, partNumber) =>
            DispatchTaskToWorkerAsync(
                workerBaseUrl,
                new WorkerCrackTaskRequest
                {
                    RequestId = queueItem.RequestId.ToString(),
                    Hash = queueItem.Hash,
                    Alphabet = _options.Alphabet,
                    MaxLength = queueItem.MaxLength,
                    PartNumber = partNumber,
                    PartCount = aliveWorkers.Count
                },
                cancellationToken));

        await Task.WhenAll(workerTasks);
    }

    private async Task DispatchTaskToWorkerAsync(string workerBaseUrl, WorkerCrackTaskRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var endpoint = new Uri(new Uri(workerBaseUrl), "/internal/api/worker/hash/crack/task");

        var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Sent crack request {RequestId} part {PartNumber}/{PartCount} to {Endpoint}",
            request.RequestId,
            request.PartNumber + 1,
            request.PartCount,
            endpoint);
    }

    private async Task<List<string>> GetAliveWorkersAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var checks = _options.WorkerBaseUrls.Select(async workerBaseUrl =>
        {
            try
            {
                var endpoint = new Uri(new Uri(workerBaseUrl), "/health");
                var response = await client.GetAsync(endpoint, cancellationToken);
                return response.StatusCode == HttpStatusCode.OK ? workerBaseUrl : null;
            }
            catch
            {
                return null;
            }
        });

        var checkResults = await Task.WhenAll(checks);
        return checkResults.Where(static x => x is not null).Cast<string>().ToList();
    }
}
