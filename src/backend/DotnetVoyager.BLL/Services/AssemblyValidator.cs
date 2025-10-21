using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services;

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
            // PEReader спеціально розроблений для читання метаданих збірок.
            // Ми створюємо його в блоці using, щоб переконатися, що ресурси звільнені.
            // LeaveOpen: true важливо, щоб ми могли повторно використовувати потік.
            using (var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen))
            {
                // Якщо цей метод не викинув виняток, значить, файл має валідний PE-заголовок
                // і є .NET-збіркою.
                if (!peReader.HasMetadata)
                {
                    return Task.FromResult(new ValidationResult(false, "The file is not a valid .NET assembly (missing metadata)."));
                }
            }

            // Важливо! Повертаємо позицію потоку на початок для подальшого читання (напр., для збереження).
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
