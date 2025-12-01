namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

public record AssemblyStatisticsDto
{
    public required int NamespaceCount { get; init; }
    public required int ClassCount { get; init; }
    public required int InterfaceCount { get; init; }
    public required int StructCount { get; init; }
    public required int MethodCount { get; init; }
}