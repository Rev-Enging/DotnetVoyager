using DotnetVoyager.BLL.Models;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

/// <summary>
/// Represents a single, isolated, and retriable analysis step
/// </summary>
public interface IAnalysisStep
{
    /// <summary>
    /// Unique name identifying this step
    /// </summary>
    string StepName { get; }

    /// <summary>
    /// Execute this step. Should be idempotent.
    /// </summary>
    /// <returns>True if successful, False if failed</returns>
    Task<bool> ExecuteAsync(
        AnalysisLocationContext analysisLocationContext,
        CancellationToken cancellationToken);
}
