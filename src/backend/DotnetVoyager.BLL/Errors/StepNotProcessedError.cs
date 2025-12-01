using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public sealed class StepNotProcessedError : Error
{
    public string AnalysisId { get; }
    public string StepName { get; }

    public StepNotProcessedError(string analysisId, string stepName)
        : base($"Step '{stepName}' has not been processed for analysis '{analysisId}'.")
    {
        AnalysisId = analysisId;
        StepName = stepName;
    }
}
