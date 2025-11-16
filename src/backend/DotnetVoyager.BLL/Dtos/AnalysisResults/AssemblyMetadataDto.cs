namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

public record AssemblyMetadataDto
{
    public required string AssemblyName { get; init; }
    public required string Version { get; init; }
    public required string TargetFramework { get; init; }
    public required string Architecture { get; init; }
    public required List<string> Dependencies { get; init; } = [];
}
