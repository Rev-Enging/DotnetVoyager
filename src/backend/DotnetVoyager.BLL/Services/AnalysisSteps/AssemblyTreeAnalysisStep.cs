using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services.Analyzers;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class AssemblyTreeAnalysisStep : IAnalysisStep
{
    private readonly IAssemblyTreeAnalyzer _assemblyTreeService;
    private readonly IStorageService _storageService;
    private readonly ILogger<AssemblyTreeAnalysisStep> _logger;

    public string StepName => AnalysisStepNames.AssemblyTree;

    public AssemblyTreeAnalysisStep(
        IAssemblyTreeAnalyzer assemblyTreeService,
        IStorageService storageService,
        ILogger<AssemblyTreeAnalysisStep> logger)
    {
        _assemblyTreeService = assemblyTreeService;
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

            var structure = await _assemblyTreeService.AnalyzeAssemblyTreeAsync(assemblyPath);

            await _storageService.SaveDataAsync(
                analysisId,
                structure,
                ProjectConstants.AssemblyTreeFileName,
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
