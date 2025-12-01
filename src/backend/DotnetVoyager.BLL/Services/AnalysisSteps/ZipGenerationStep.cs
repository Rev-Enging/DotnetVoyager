using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class ZipGenerationStep : IAnalysisStep
{
    private readonly IFullDecompilationService _decompilationService;
    private readonly IStorageService _storageService;
    private readonly ILogger<ZipGenerationStep> _logger;

    public string StepName => AnalysisStepNames.ZipGeneration;

    public ZipGenerationStep(
        IFullDecompilationService decompilationService,
        IStorageService storageService,
        ILogger<ZipGenerationStep> logger)
    {
        _decompilationService = decompilationService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(
        AnalysisLocationContext analysisLocationContext,
        CancellationToken cancellationToken)
    {
        var analysisId = analysisLocationContext.AnalysisId;
        var assemblyPath = analysisLocationContext.AssemblyPath;

        try
        {
            _logger.LogInformation("Executing {Step} for {AnalysisId}", StepName, analysisId);

            var analysisFolder = _storageService.GetAnalysisDirectoryPath(analysisId);
            var tempSourcePath = Path.Combine(analysisFolder, "decompiled_source_temp");
            var zipPath = Path.Combine(analysisFolder, ProjectConstants.DecompiledZipFileName);

            try
            {
                if (Directory.Exists(tempSourcePath))
                    Directory.Delete(tempSourcePath, true);
                Directory.CreateDirectory(tempSourcePath);

                await _decompilationService.DecompileProjectAsync(
                    assemblyPath, tempSourcePath, cancellationToken);

                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(
                    tempSourcePath, zipPath, CompressionLevel.Fastest, false);

                _logger.LogInformation("Completed {Step} for {AnalysisId}", StepName, analysisId);
                return true;
            }
            finally
            {
                if (Directory.Exists(tempSourcePath))
                {
                    try { Directory.Delete(tempSourcePath, true); }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup temp directory");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {Step} for {AnalysisId}", StepName, analysisId);
            return false;
        }
    }
}