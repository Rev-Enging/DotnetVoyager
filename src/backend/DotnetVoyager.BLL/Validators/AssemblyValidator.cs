using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Validators;

public record ValidationResult(bool IsValid, string? ErrorMessage = null);

public interface IAssemblyValidator
{
    Task<ValidationResult> ValidateAsync(Stream stream);
}

public class AssemblyValidator : IAssemblyValidator
{
    public Task<ValidationResult> ValidateAsync(Stream stream)
    {
        try
        {
            // LeaveOpen: true is important so that we can reuse the stream later.
            using (var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen))
            {
                // If this does not throw an exception, the file has a valid PE header
                // and is potentially a .NET assembly.
                if (!peReader.HasMetadata)
                {
                    return Task.FromResult(new ValidationResult(
                        IsValid: false, 
                        ErrorMessage: "The file is not a valid .NET assembly (missing metadata)."));
                }
            }

            // Reset the stream position to the beginning for further reading (e.g., for saving).
            stream.Position = 0;
            return Task.FromResult(new ValidationResult(true));
        }
        catch (BadImageFormatException)
        {
            return Task.FromResult(new ValidationResult(false, "The file is not a valid .NET assembly."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ValidationResult(false, $"An error occurred while validating the file: {ex.Message}"));
        }
    }
}