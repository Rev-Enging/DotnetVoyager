using FluentResults;
using FluentValidation.Results;

namespace DotnetVoyager.BLL.Errors;

public class ValidationError : Error
{
    public ValidationResult ValidationResult { get; init; }

    public ValidationError(ValidationResult validationResult)
    {
        ValidationResult = validationResult;
    }
}
