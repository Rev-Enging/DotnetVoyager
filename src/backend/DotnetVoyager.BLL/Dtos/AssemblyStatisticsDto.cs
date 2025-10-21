namespace DotnetVoyager.BLL.Dtos;

public class AssemblyStatisticsDto
{
    public required int NamespaceCount { get; init; }
    public required int ClassCount { get; init; }
    public required int InterfaceCount { get; init; }
    public required int StructCount { get; init; }
    public required int MethodCount { get; init; }
    public required int DecompiledLinesOfCode { get; init; }
    public required int IlInstructionCount { get; init; }
}