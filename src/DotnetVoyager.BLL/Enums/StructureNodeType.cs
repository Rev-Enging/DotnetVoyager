using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StructureNodeType
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