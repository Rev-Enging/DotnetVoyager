namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

public class AssemblyDependenciesDto
{
    public required string AssemblyName { get; init; }
    public required string Version { get; init; }
    public required string Culture { get; init; }
    public required string PublicKeyToken { get; init; }
    public List<AssemblyReferenceDto> References { get; init; } = [];
}

public class AssemblyReferenceDto
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string Culture { get; init; }
    public required string PublicKeyToken { get; init; }
}
