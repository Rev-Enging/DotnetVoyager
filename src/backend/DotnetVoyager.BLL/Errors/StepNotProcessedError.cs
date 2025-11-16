using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public sealed class StepNotProcessedError : Error
{
    public string AnalysisId { get; init; }
    public string StepName { get; init; }

    public StepNotProcessedError(string analysisId, string stepName)
        : base($"Step '{stepName}' was not processed for analysis {analysisId}.")
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException("Step name cannot be null or whitespace.", nameof(stepName));
        }

        StepName = stepName;
        AnalysisId = analysisId;
    }
}
