using DotnetVoyager.DAL.Enums;
using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public class StepNotCompletedError : Error
{
    public string StepName { get; }
    public AnalysisStepStatus CurrentStatus { get; }

    public StepNotCompletedError(
        string stepName,
        AnalysisStepStatus currentStatus,
        string? errorMessage = null)
        : base($"Step '{stepName}' is not completed. Current status: {currentStatus}")
    {
        StepName = stepName;
        CurrentStatus = currentStatus;

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Metadata.Add("ErrorMessage", errorMessage);
        }
    }
}
