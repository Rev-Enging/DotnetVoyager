using DotnetVoyager.DAL.Enums;
using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public sealed class StepCannotBeRetriedError : Error
{
    public string StepName { get; }
    public AnalysisStepStatus CurrentStatus { get; }

    public StepCannotBeRetriedError(string stepName, AnalysisStepStatus currentStatus)
        : base($"Step '{stepName}' cannot be retried. Current status: {currentStatus}. Only failed steps can be retried.")
    {
        StepName = stepName;
        CurrentStatus = currentStatus;
    }
}