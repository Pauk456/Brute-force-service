using System.Net.Http.Json;
using Manager.Application.Models;
using Manager.Contracts;
using Manager.Domain.Entities;
using Manager.Infrastructure.Options;
using Manager.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace Manager.Application.Services;

public sealed class HashCrackService
{
    private readonly InMemoryCrackRequestRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ManagerOptions _options;

    public HashCrackService(
        InMemoryCrackRequestRepository repository,
        IHttpClientFactory httpClientFactory,
        IOptions<ManagerOptions> options)
    {
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public Task<StartCrackResponseDto> StartCrackAsync(StartCrackRequestDto request, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        var state = new CrackRequestState(requestId, DateTime.UtcNow, _options.WorkerCount);
        _repository.Add(state);

        var workerTasks = Enumerable.Range(0, _options.WorkerCount)
            .Select(partNumber => DispatchTaskToWorkerAsync(
                new WorkerCrackTaskRequest
                {
                    RequestId = requestId.ToString(),
                    Hash = request.Hash.ToLowerInvariant(),
                    Alphabet = _options.Alphabet,
                    MaxLength = request.MaxLength,
                    PartNumber = partNumber,
                    PartCount = _options.WorkerCount
                },
                cancellationToken));

        _ = Task.WhenAll(workerTasks).ContinueWith(_ => { }, TaskScheduler.Default);

        return Task.FromResult(new StartCrackResponseDto { RequestId = requestId.ToString() });
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

        return state.TryAddPartResult(result.PartNumber, result.Words);
    }

    public void MarkTimedOutRequestsAsError()
    {
        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-_options.RequestTimeoutSeconds);
        foreach (var state in _repository.GetAllInProgress().Where(x => x.CreatedAtUtc <= timeoutThreshold))
        {
            state.MarkAsError();
        }
    }

    private async Task DispatchTaskToWorkerAsync(WorkerCrackTaskRequest request, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var endpoint = $"{_options.WorkerBaseUrl}/internal/api/worker/hash/crack/task";
        await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        Console.WriteLine($"[INFO] Sent request to URL: {endpoint} with request: {System.Text.Json.JsonSerializer.Serialize(request)}");
    }
}
