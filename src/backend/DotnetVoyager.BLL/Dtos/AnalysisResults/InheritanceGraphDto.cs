using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Dtos.AnalysisResults;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InheritanceGraphNodeType
{
    Class,
    Interface,
    Struct,
}

/// <summary>
/// Represents the complete inheritance graph for the assembly.
/// </summary>
public class InheritanceGraphDto
{
    public required List<InheritanceGraphNodeDto> Nodes { get; init; } = [];
    public required List<InheritanceGraphEdgeDto> Edges { get; init; } = [];
}

/// <summary>
/// Represents a single type (Class, Interface, Struct) in the graph.
/// </summary>
public class InheritanceGraphNodeDto
{
    /// <summary>
    /// The Unique Primary Key for the graph.
    /// For internal types: string-representation of TokenId (e.g., "112233").
    /// For external types: FullName (e.g., "System.Object").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The metadata token. This is the key to link with the StructureNodeDto tree.
    /// Will be 0 for external types (like System.Object) that are not defined in this assembly.
    /// </summary>
    public required int TokenId { get; init; }

    /// <summary>
    /// The full, namespace-qualified name (e.g., "MyNamespace.MyClass").
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// The short, simple name of the type (e.g., "MyClass").
    /// </summary>
    public required string ShortName { get; init; }

    /// <summary>
    /// The kind of type.
    /// </summary>
    public required InheritanceGraphNodeType Type { get; init; }

    /// <summary>
    /// True if this type is from a referenced, external assembly (e.g., System.Runtime).
    /// </summary>
    public bool IsExternal { get; init; }
}

/// <summary>
/// Represents a single relationship (edge) between two nodes.
/// </summary>
public class InheritanceGraphEdgeDto
{
    /// <summary>
    /// A unique ID for the edge (e.g., "112233_inherits_System.Object").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The 'Id' of the source node (the child class).
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// The 'Id' of the target node (the base class or interface).
    /// </summary>
    public required string Target { get; init; }
}