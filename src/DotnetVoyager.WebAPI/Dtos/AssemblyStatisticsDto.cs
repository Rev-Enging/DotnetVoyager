namespace DotnetVoyager.WebAPI.Dtos;

public class AssemblyStatisticsDto
{
    public int NamespaceCount { get; set; }
    public int ClassCount { get; set; }
    public int InterfaceCount { get; set; }
    public int StructCount { get; set; }
    public int MethodCount { get; set; }
    // Note: Lines of code is a complex metric. Returning 0 for now.
    public int LinesOfCode { get; set; }
}