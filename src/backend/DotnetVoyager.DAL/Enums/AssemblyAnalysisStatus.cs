using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AssemblyAnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}
