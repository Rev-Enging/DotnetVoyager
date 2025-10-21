namespace DotnetVoyager.BLL.Dtos;

public class AssemblyDetailsDto
{
    public required AssemblyMetadataDto MetadataDto { get; init; }
    public required AssemblyStatisticsDto Statistics { get; init; }
}