using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services.Analyzers;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class AssemblyDependencyAnalysisStep : IAnalysisStep
{
    private readonly IStorageService _storageService;
    private readonly IAssemblyReferenceAnalyzer _assemblyReferenceService;
    private readonly ILogger<AssemblyDependencyAnalysisStep> _logger;

    public string StepName => AnalysisStepNames.AssemblyDependencies;

    public AssemblyDependencyAnalysisStep(
        IAssemblyReferenceAnalyzer assemblyReferenceService,
        IStorageService storageService,
        ILogger<AssemblyDependencyAnalysisStep> logger)
    {
        _assemblyReferenceService = assemblyReferenceService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(AnalysisLocationContext analysisLocationContext, CancellationToken cancellationToken)
    {
        var analysisId = analysisLocationContext.AnalysisId;
        var assemblyPath = analysisLocationContext.AssemblyPath;

        try
        {
            _logger.LogInformation("Executing {Step} for {AnalysisId}", StepName, analysisId);

            var assemblyDependencies = await _assemblyReferenceService.AnalyzeReferences(assemblyPath);

            await _storageService.SaveDataAsync(
                analysisId,
                assemblyDependencies,
                ProjectConstants.AssemblyDependenciesFileName,
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
