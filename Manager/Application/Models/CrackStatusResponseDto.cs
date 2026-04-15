namespace Manager.Application.Models;

public sealed class CrackStatusResponseDto
{
    public required string Status { get; init; }
    public IReadOnlyCollection<string>? Data { get; init; }
}
