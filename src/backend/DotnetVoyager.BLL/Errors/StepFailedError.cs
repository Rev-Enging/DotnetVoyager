using FluentResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnetVoyager.BLL.Errors;

public sealed class StepFailedError : Error
{
    public string StepName { get; }
    public string? ErrorMessage { get; }

    public StepFailedError(string stepName, string? errorMessage = null)
        : base($"Step '{stepName}' has failed.")
    {
        if (string.IsNullOrWhiteSpace(stepName))
        {
            throw new ArgumentException("Step name cannot be null or whitespace.", nameof(stepName));
        }

        StepName = stepName;
        ErrorMessage = errorMessage;

        Metadata["StepName"] = stepName;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            Metadata["ErrorMessage"] = errorMessage;
        }
    }
}
