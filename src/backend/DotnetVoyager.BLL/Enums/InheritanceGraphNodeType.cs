using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InheritanceGraphNodeType
{
    Class,
    Interface,
    Struct,
}
