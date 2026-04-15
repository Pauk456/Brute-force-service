namespace Manager.Application.Models;

public sealed class StartCrackRequestDto
{
    public required string Hash { get; init; }
    public required int MaxLength { get; init; }
}
