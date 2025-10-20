using DotnetVoyager.BLL.Enums;
using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public class AnalysisNotCompletedError : Error
{
    public AnalysisStatus CurrentStatus { get; }

    public AnalysisNotCompletedError(AnalysisStatus currentStatus)
        : base($"Analysis is not yet completed. Current status: {currentStatus}")
    {
        if (currentStatus == AnalysisStatus.Completed)
        {
            throw new ArgumentException("Current status cannot be 'Completed' for AnalysisNotCompletedError.", nameof(currentStatus));
        }

        CurrentStatus = currentStatus;
    }
}
