using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisStepName
{
    Metadata,
    Statistics,
    AssemblyTree,
    AssemblyDependencies,
    InheritanceGraph,
    ZipGeneration
}
