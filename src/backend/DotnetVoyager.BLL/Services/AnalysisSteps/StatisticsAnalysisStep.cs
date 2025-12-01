using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services.Analyzers;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class StatisticsAnalysisStep : IAnalysisStep
{
    private readonly IStatisticsAnalyzer _statisticsService;
    private readonly IStorageService _storageService;
    private readonly ILogger<StatisticsAnalysisStep> _logger;

    public string StepName => AnalysisStepNames.Statistics;

    public StatisticsAnalysisStep(
        IStatisticsAnalyzer statisticsService,
        IStorageService storageService,
        ILogger<StatisticsAnalysisStep> logger)
    {
        _statisticsService = statisticsService;
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

            var statistics = await _statisticsService.GetAssemblyStatisticsAsync(assemblyPath);

            await _storageService.SaveDataAsync(
                analysisId,
                statistics,
                ProjectConstants.AnalysisStatisticsFileName,
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