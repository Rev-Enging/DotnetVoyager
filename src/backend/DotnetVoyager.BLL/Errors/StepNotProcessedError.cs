using FluentResults;

namespace DotnetVoyager.BLL.Errors;

public sealed class StepNotProcessedError : Error
{
    public string StepName { get; }

    public StepNotProcessedError(string stepName)
        : base($"Step '{stepName}' was not processed.")
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException("Step name cannot be null or whitespace.", nameof(stepName));
        }

        StepName = stepName;
    }
}
