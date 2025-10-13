namespace DotnetVoyager.WebAPI.Dtos;

public class AssemblyDetailsDto
{
    public required string AssemblyName { get; set; }
    public required string Version { get; set; }
    public required string TargetFramework { get; set; }
    public required string Architecture { get; set; }
    public List<string> Dependencies { get; set; } = [];
    public AssemblyStatisticsDto Statistics { get; set; } = new();
}