using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.MediatR.Commands.UploadAssembly;
using DotnetVoyager.WebAPI.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DotnetVoyager.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalysisController> _logger;

    public AnalysisController(
        IMediator mediator,
        ILogger<AnalysisController> logger)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadAssemblyResultDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProjectConstants.MaxAssemblySizeInBytes)]
    public async Task<IActionResult> Upload([FromForm] UploadAssemblyRequestDto request)
    {
        var bllDto = request.ToBllDto();

        var result = await _mediator.Send(new UploadAssemblyCommand(bllDto));

        if (result.IsSuccess)
        {
            return Accepted(result.Value);
        }

        if (result.HasError<ValidationError>())
        {
            var validationError = result.Errors.OfType<ValidationError>().First();
            return HandleValidationError(validationError);
        }

        return Problem(
            detail: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }

    private IActionResult HandleValidationError(ValidationError validationError)
    {
        var errors = validationError.ValidationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return ValidationProblem(new ValidationProblemDetails(errors));
    }
}






/*    [HttpGet("{analysisId}/structure")]
    [ProducesResponseType(typeof(List<StructureNodeDto>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public IActionResult GetStructure([FromRoute] string analysisId)
    {
        try
        {
            var targetDirectoryPath = _storageService.GetAnalysisDirectoryPath(analysisId);

            if (!Directory.Exists(targetDirectoryPath))
            {
                return NotFound($"Analysis session with ID '{analysisId}' not found or has expired.");
            }

            var assemblyPath = Directory.GetFiles(targetDirectoryPath, "*.dll").FirstOrDefault() ??
                               Directory.GetFiles(targetDirectoryPath, "*.exe").FirstOrDefault();

            if (assemblyPath == null)
            {
                return NotFound($"No assembly file (.dll or .exe) found for analysis session '{analysisId}'.");
            }

            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            var structure = new List<StructureNodeDto>();

            var namespaceGroups = assembly.MainModule.Types
                .Where(t => t.IsPublic)
                .GroupBy(t => t.Namespace);

            foreach (var group in namespaceGroups)
            {
                var namespaceNode = new StructureNodeDto
                {
                    Name = group.Key ?? "[No Namespace]",
                    Type = "namespace",
                    Children = new List<StructureNodeDto>()
                };

                foreach (var type in group)
                {
                    var classNode = new StructureNodeDto
                    {
                        Name = type.Name,
                        Type = type.IsClass ? "class" : "interface",
                        Token = type.MetadataToken.ToInt32(),
                        Children = new List<StructureNodeDto>()
                    };

                    foreach (var method in type.Methods.Where(m => m.IsPublic && !m.IsConstructor))
                    {
                        classNode.Children.Add(new StructureNodeDto
                        {
                            Name = method.Name,
                            Type = "method",
                            Token = method.MetadataToken.ToInt32()
                        });
                    }

                    namespaceNode.Children.Add(classNode);
                }

                structure.Add(namespaceNode);
            }

            return Ok(structure);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to analyze assembly: {ex.Message}");
        }
    }*/
/*
    [HttpGet("{analysisId}/code")]
    [ProducesResponseType(typeof(DecompiledCodeDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public IActionResult GetDecompiledCode([FromRoute] string analysisId, [FromQuery] int token)
    {
        try
        {
            var targetDirectoryPath = _storageService.GetAnalysisDirectoryPath(analysisId);
            if (!Directory.Exists(targetDirectoryPath))
            {
                return NotFound($"Analysis session with ID '{analysisId}' not found or has expired.");
            }

            var assemblyPath = Directory.GetFiles(targetDirectoryPath, "*.dll").FirstOrDefault() ??
                               Directory.GetFiles(targetDirectoryPath, "*.exe").FirstOrDefault();

            if (assemblyPath == null)
            {
                return NotFound($"No assembly file (.dll or .exe) found for analysis session '{analysisId}'.");
            }

            var handle = MetadataTokens.EntityHandle(token);

            // Налаштовуємо resolver для роботи без залежностей
            var resolver = new UniversalAssemblyResolver(
                assemblyPath,
                throwOnError: false,  // Не кидати виняток при відсутності залежностей
                targetFramework: null
            );

            // Додаємо директорію з поточною збіркою
            resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

            // Налаштовуємо DecompilerSettings
            var settings = new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false  // Не падати при помилках резолвінгу
            };

            // Створюємо декомпілятор з налаштованим resolver
            var decompiler = new CSharpDecompiler(assemblyPath, resolver, settings);

            var csharpCode = decompiler.DecompileAsString(handle);

            // IL-код через Mono.Cecil
            string ilCode;
            var readerParameters = new ReaderParameters
            {
                AssemblyResolver = new DefaultAssemblyResolver() // Cecil теж потребує resolver
            };

            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
            var member = assembly.MainModule.LookupToken(token);

            switch (member)
            {
                case MethodDefinition method when method.HasBody:
                    var sb = new StringBuilder();
                    foreach (var instruction in method.Body.Instructions)
                    {
                        sb.AppendLine(instruction.ToString());
                    }
                    ilCode = sb.ToString();
                    break;
                case MethodDefinition _:
                    ilCode = "This method does not have a method body (e.g., it's abstract or in an interface).";
                    break;
                default:
                    ilCode = "IL code is available only for methods.";
                    break;
            }

            var response = new DecompiledCodeDto
            {
                CSharpCode = csharpCode,
                IlCode = ilCode
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to decompile code: {ex.Message}");
        }
    }

    [HttpGet("{analysisId}/dependencies")]
    [ProducesResponseType(typeof(DependencyGraphDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public IActionResult GetAssemblyDependencies([FromRoute] string analysisId)
    {
        try
        {
            var targetDirectoryPath = _storageService.GetAnalysisDirectoryPath(analysisId);

            if (!Directory.Exists(targetDirectoryPath))
            {
                return NotFound($"Analysis session with ID '{analysisId}' not found or has expired.");
            }

            var assemblyPath = Directory.GetFiles(targetDirectoryPath, "*.dll").FirstOrDefault() ??
                               Directory.GetFiles(targetDirectoryPath, "*.exe").FirstOrDefault();

            if (assemblyPath == null)
            {
                return NotFound($"No assembly file (.dll or .exe) found for analysis session '{analysisId}'.");
            }

            var dependencyGraph = _dependencyAnalyzer.AnalyzeAssemblyDependencies(assemblyPath);

            return Ok(dependencyGraph);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to analyze dependencies: {ex.Message}");
        }
    }*/








    // NOTTHIS!!!!
    // !!!!
    // !!!!
    /*[HttpGet("{analysisId}/details")]
    [ProducesResponseType(typeof(AssemblyDetailsDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public IActionResult GetAssemblyDetails([FromRoute] string analysisId)
    {
        // Використовуємо допоміжний метод, щоб уникнути дублювання коду
        if (!TryGetPrimaryAssemblyPath(analysisId, out var assemblyPath, out var errorResult))
        {
            return errorResult;
        }

        try
        {
            // Кожен раз викликаємо сервіс для обчислення даних
            var details = _metadataService.GetAssemblyDetails(assemblyPath!);
            return Ok(details);
        }
        catch (Exception ex)
        {
            // It's a good practice to log the exception here
            return BadRequest($"Failed to get assembly details: {ex.Message}");
        }
    }
*/




/*    private bool TryGetPrimaryAssemblyPath(string analysisId, out string? assemblyPath, out IActionResult errorResult)
    {
        var targetDirectoryPath = _storageService.GetAnalysisDirectoryPath(analysisId);
        if (!Directory.Exists(targetDirectoryPath))
        {
            errorResult = NotFound($"Analysis session with ID '{analysisId}' not found or has expired.");
            assemblyPath = null;
            return false;
        }

        assemblyPath = Directory.GetFiles(targetDirectoryPath, "*.dll").FirstOrDefault() ??
                       Directory.GetFiles(targetDirectoryPath, "*.exe").FirstOrDefault();

        if (assemblyPath == null)
        {
            errorResult = NotFound($"No assembly file (.dll or .exe) found for analysis session '{analysisId}'.");
            return false;
        }

        errorResult = null!;
        return true;
    }
}
*/

/*    [HttpGet("{analysisId}/code")]
    [ProducesResponseType(typeof(DecompiledCodeDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    public IActionResult GetDecompiledCode([FromRoute] string analysisId, [FromQuery] int token)
    {
        try
        {
            var targetDirectoryPath = _storageService.GetAnalysisDirectoryPath(analysisId);

            if (!Directory.Exists(targetDirectoryPath))
            {
                return NotFound($"Analysis session with ID '{analysisId}' not found or has expired.");
            }

            var assemblyPath = Directory.GetFiles(targetDirectoryPath, "*.dll").FirstOrDefault() ??
                               Directory.GetFiles(targetDirectoryPath, "*.exe").FirstOrDefault();

            if (assemblyPath == null)
            {
                return NotFound($"No assembly file (.dll or .exe) found for analysis session '{analysisId}'.");
            }

            // 1. Створюємо EntityHandle безпосередньо з int-значення токена
            var handle = MetadataTokens.EntityHandle(token);

            // 2. Декомпілюємо C# код. Цей метод сам визначить, що це - клас, метод чи властивість.
            var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings());
            var csharpCode = decompiler.DecompileAsString(handle);

            // 3. Отримуємо IL-код, використовуючи Mono.Cecil, як і раніше.
            //    Ця частина була правильною і залишається корисною для диференціації.
            string ilCode;
            var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            var member = assembly.MainModule.LookupToken(token);

            switch (member)
            {
                case MethodDefinition method when method.HasBody:
                    var sb = new StringBuilder();
                    foreach (var instruction in method.Body.Instructions)
                    {
                        sb.AppendLine(instruction.ToString());
                    }
                    ilCode = sb.ToString();
                    break;
                case MethodDefinition _:
                    ilCode = "This method does not have a method body (e.g., it's abstract or in an interface).";
                    break;
                default:
                    ilCode = "IL code is available only for methods.";
                    break;
            }

            var response = new DecompiledCodeDto
            {
                CSharpCode = csharpCode,
                IlCode = ilCode
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to decompile code: {ex.Message}");
        }
    }*/