using DotnetVoyager.BLL.Enums;
using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public class AnalysisNotCompletedError : Error
{
    public AssemblyAnalysisStatus CurrentStatus { get; }

    public AnalysisNotCompletedError(AssemblyAnalysisStatus currentStatus)
        : base($"Analysis is not yet completed. Current status: {currentStatus}")
    {
        if (currentStatus == AssemblyAnalysisStatus.Completed)
        {
            throw new ArgumentException("Current status cannot be 'Completed' for AnalysisNotCompletedError.", nameof(currentStatus));
        }

        CurrentStatus = currentStatus;
    }
}
