using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Services;
using FluentValidation;

namespace DotnetVoyager.BLL.Validators;

public class UploadRequestValidator : AbstractValidator<UploadAssemblyDto>
{
    private readonly IAssemblyValidator _assemblyValidator;

    public UploadRequestValidator(IAssemblyValidator assemblyValidator)
    {
        _assemblyValidator = assemblyValidator;

        RuleFor(x => x.File)
            .NotNull()
            .WithMessage("No file was uploaded.");

        When(x => x.File != null, () =>
        {
            RuleFor(x => x.File)
                .MustAsync(async (file, cancellationToken) =>
                {
                    var validationResult = await _assemblyValidator.ValidateAsync(file.FileStream);
                    return validationResult.IsValid;
                })
                .WithMessage("The uploaded file is not a valid .NET assembly.");
        });
    }
}
