using DotnetVoyager.WebAPI.Dtos;
using DotnetVoyager.WebAPI.Services;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Microsoft.AspNetCore.Mvc;
using Mono.Cecil;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DotnetVoyager.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly IStorageService _storageService;

    public AnalysisController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResponseDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Upload([FromForm] List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files were uploaded.");
        }

        var analysisId = Guid.NewGuid().ToString();

        // Вся складна логіка тепер в одному виклику!
        await _storageService.SaveAnalysisFilesAsync(files, analysisId);

        var response = new UploadResponseDto
        {
            AnalysisId = analysisId
        };

        return Ok(response);
    }

    [HttpGet("{analysisId}/structure")]
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
    }

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
    }
}
