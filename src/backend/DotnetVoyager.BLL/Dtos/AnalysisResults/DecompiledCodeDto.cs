namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

public class DecompiledCodeDto
{
    public required string CSharpCode { get; init; }
    public required string IlCode { get; init; }
}
