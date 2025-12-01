using DotnetVoyager.BLL.Constants;

namespace DotnetVoyager.BLL.Exceptions;

public sealed class AnalysisStepNotExistException : Exception
{
    public string StepName { get; init; }

    public AnalysisStepNotExistException(string stepName)
        : base($"The analysis step '{stepName}' does not exist.")
    {
        StepName = stepName;
    }

    public static void ThrowIfStepNotExist(string stepName)
    {
        if (!AnalysisStepNames.IsValidStep(stepName))
        {
            throw new AnalysisStepNotExistException(stepName);
        }
    }
}
