namespace Manager.Application.Models;

public sealed class CrackRequestDto
{
    public required string Hash { get; init; }
    public required int MaxLength { get; init; }
}
