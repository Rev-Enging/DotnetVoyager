using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Enums;

/// <summary>
/// Represents the processing state of an analysis task.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnalysisStatus
{
    /// <summary>
    /// Task is created and waiting in the queue.
    /// </summary>
    Pending,

    /// <summary>
    /// A worker has dequeued the task and is actively processing it.
    /// </summary>
    Processing,

    /// <summary>
    /// The analysis completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// An error occurred during processing.
    /// </summary>
    Failed
}
