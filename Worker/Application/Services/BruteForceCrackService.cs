using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using Worker.Contracts;
using Worker.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Worker.Application.Services;

public sealed class BruteForceCrackService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WorkerOptions _options;

    public BruteForceCrackService(IHttpClientFactory httpClientFactory, IOptions<WorkerOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task ProcessTaskAsync(WorkerCrackTaskRequest request, CancellationToken cancellationToken)
    {
        var words = ExecutePartitionSearch(request);
        var result = new WorkerCrackResultResponse
        {
            RequestId = request.RequestId,
            PartNumber = request.PartNumber,
            Words = words
        };

        await SendResultAsync(result, cancellationToken);
    }

    private static List<string> ExecutePartitionSearch(WorkerCrackTaskRequest request)
    {
        var alphabet = request.Alphabet.ToCharArray();
        var ranges = BuildRanges(request.MaxLength, alphabet.Length);
        var total = ranges[^1].EndExclusive;

        var start = total * request.PartNumber / request.PartCount;
        var end = total * (request.PartNumber + 1) / request.PartCount;
        var result = new List<string>();
        var targetHash = request.Hash.ToLowerInvariant();

        using var md5 = MD5.Create();
        for (var position = start; position < end; position++)
        {
            var candidate = ResolveWord(position, ranges, alphabet);
            if (ComputeHash(candidate, md5) == targetHash)
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private static string ComputeHash(string word, MD5 md5)
    {
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(word));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string ResolveWord(long position, IReadOnlyList<LengthRange> ranges, IReadOnlyList<char> alphabet)
    {
        var selectedRange = ranges.First(x => position >= x.StartInclusive && position < x.EndExclusive);
        var localPosition = position - selectedRange.StartInclusive;
        var chars = new char[selectedRange.Length];

        for (var i = selectedRange.Length - 1; i >= 0; i--)
        {
            chars[i] = alphabet[(int)(localPosition % alphabet.Count)];
            localPosition /= alphabet.Count;
        }

        return new string(chars);
    }

    private static List<LengthRange> BuildRanges(int maxLength, int alphabetSize)
    {
        var ranges = new List<LengthRange>();
        long cursor = 0;
        long power = alphabetSize;

        for (var length = 1; length <= maxLength; length++)
        {
            var end = cursor + power;
            ranges.Add(new LengthRange(length, cursor, end));
            cursor = end;
            power *= alphabetSize;
        }

        return ranges;
    }

    private async Task SendResultAsync(WorkerCrackResultResponse response, CancellationToken cancellationToken)
    {
        var serializer = new XmlSerializer(typeof(WorkerCrackResultResponse));
        await using var memory = new MemoryStream();
        serializer.Serialize(memory, response);
        memory.Position = 0;
        using var content = new StreamContent(memory);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");

        using var request = new HttpRequestMessage(HttpMethod.Patch, _options.ManagerCallbackUrl)
        {
            Content = content
        };

        var client = _httpClientFactory.CreateClient();
        await client.SendAsync(request, cancellationToken);
    }

    private sealed record LengthRange(int Length, long StartInclusive, long EndExclusive);
}
