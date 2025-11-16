using DotnetVoyager.DAL.Enums;

namespace DotnetVoyager.BLL.Dtos;

public record AnalysisStatusDto
{
    public required string AnalysisId { get; init; }
    public required AnalysisOverallStatus OverallStatus { get; init; }
    public required DateTime LastUpdatedUtc { get; init; }
    public required List<StepStatusDto> Steps { get; init; }
}

public record StepStatusDto
{
    public required string StepName { get; init; }
    public required AnalysisStepStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime? StartedUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
}