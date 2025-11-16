using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services.Analyzers;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class InheritanceGraphAnalysisStep : IAnalysisStep
{
    private readonly IStorageService _storageService;
    private readonly IInheritanceGraphBuilderService _graphService;
    private readonly ILogger<InheritanceGraphAnalysisStep> _logger;

    public string StepName => AnalysisStepNames.InheritanceGraph;

    public InheritanceGraphAnalysisStep(
        IInheritanceGraphBuilderService graphService,
        IStorageService storageService,
        ILogger<InheritanceGraphAnalysisStep> logger)
    {
        _graphService = graphService;
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

            var graph = await _graphService.BuildGraphAsync(assemblyPath);

            await _storageService.SaveDataAsync(
                analysisId,
                graph,
                ProjectConstants.AnalysisInheritanceGraphFileName,
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
