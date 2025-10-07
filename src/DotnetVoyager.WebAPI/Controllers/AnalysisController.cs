using DotnetVoyager.WebAPI.Dtos;
using DotnetVoyager.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Mono.Cecil;

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
}
