using DotnetVoyager.BLL.Enums;

namespace DotnetVoyager.BLL.Dtos;

public record AnalysisStatusDto
{
    public required string AnalysisId { get; set; }

    public required AssemblyAnalysisStatus Status { get; set; } = AssemblyAnalysisStatus.Pending;

    public string? ErrorMessage { get; set; }

    public ZipGenerationStatus ZipStatus { get; set; } = ZipGenerationStatus.NotStarted;

    public string? ZipErrorMessage { get; set; }

    public DateTime LastUpdatedUtc { get; set; }
}