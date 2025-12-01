namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

public record AssemblyDetailsDto
{
    public required AssemblyMetadataDto MetadataDto { get; init; }
    public required AssemblyStatisticsDto Statistics { get; init; }
}