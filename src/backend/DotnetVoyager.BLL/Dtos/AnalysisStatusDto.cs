using DotnetVoyager.BLL.Enums;

namespace DotnetVoyager.BLL.Dtos;

public record AnalysisStatusDto
{
    public required string AnalysisId { get; init; }

    public required AnalysisStatus Status { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime LastUpdatedUtc { get; init; }
}