using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services.Analyzers;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class MetadataAnalysisStep : IAnalysisStep
{
    private readonly IStorageService _storageService;
    private readonly IMetadataReaderService _metadataService;
    private readonly ILogger<MetadataAnalysisStep> _logger;

    public string StepName => AnalysisStepNames.Metadata;

    public MetadataAnalysisStep(
        IMetadataReaderService metadataService,
        IStorageService storageService,
        ILogger<MetadataAnalysisStep> logger)
    {
        _metadataService = metadataService;
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

            var metadata = await _metadataService.GetAssemblyMetadataAsync(
                assemblyPath, cancellationToken);

            await _storageService.SaveDataAsync(
                analysisId,
                metadata,
                ProjectConstants.AnalysisMetadataFileName,
                cancellationToken);

            _logger.LogInformation("Completed {Step} for {AnalysisId}", StepName, analysisId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {Step} for {AnalysisId}", StepName, analysisId);
            return false;
        }
    }
}
