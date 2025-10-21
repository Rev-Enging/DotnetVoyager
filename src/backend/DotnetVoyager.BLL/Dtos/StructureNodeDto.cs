using DotnetVoyager.BLL.Enums;

namespace DotnetVoyager.BLL.Dtos;

public class StructureNodeDto
{
    public required string Name { get; init; }
    public int Token { get; init; }
    public required StructureNodeType Type { get; init; }
    public List<StructureNodeDto>? Children { get; init; }
}
