using System.Text.Json.Serialization;

namespace DotnetVoyager.DAL.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisStepStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    NotProcessed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisOverallStatus
{
    Pending,
    Processing,
    PartiallyCompleted,  // Some steps done, some failed
    Completed,           // All required steps done
    Failed               // Critical steps failed
}
