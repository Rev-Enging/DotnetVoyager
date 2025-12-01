using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssemblyTreeNodeType
{
    Assembly,
    Namespace,
    Class,
    Interface,
    Struct,
    Method,
    Property,
    Field
}

public record AssemblyTreeDto
{
    public required AssemblyTreeNodeDto Root { get; init; }
}

public record AssemblyTreeNodeDto
{
    public required string Name { get; init; }
    public required AssemblyTreeNodeType Type { get; init; }
    public int Token { get; init; }
    public List<AssemblyTreeNodeDto>? Children { get; init; }
}
