namespace DotnetVoyager.BLL.Dtos;

public class StructureNodeDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public int Token { get; init; }
    public List<StructureNodeDto>? Children { get; init; }
}
