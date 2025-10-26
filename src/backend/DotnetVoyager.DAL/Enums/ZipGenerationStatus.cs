using System.Text.Json.Serialization;

namespace DotnetVoyager.BLL.Enums;

/// <summary>
/// Represents the processing state of a full source code ZIP export.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZipGenerationStatus
{
    /// <summary>
    /// The ZIP generation has not been requested.
    /// </summary>
    NotStarted,

    /// <summary>
    /// The task is created and waiting in the queue.
    /// </summary>
    Pending,

    /// <summary>
    /// A worker has dequeued the task and is actively generating the zip.
    /// </summary>
    Processing,

    /// <summary>
    /// The ZIP generation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// An error occurred during ZIP generation.
    /// </summary>
    Failed
}