using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetStatistic;

public record GetStatisticQuery(string AnalysisId) : IRequest<Result<AssemblyStatisticsDto>>;

public class GetStatisticHandler : IRequestHandler<GetStatisticQuery, Result<AssemblyStatisticsDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<GetStatisticHandler> _logger;

    public GetStatisticHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetStatisticHandler> logger)
    {
        _storageService = storageService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Result<AssemblyStatisticsDto>> Handle(
        GetStatisticQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string stepName = AnalysisStepNames.Statistics;
            const string fileName = ProjectConstants.AnalysisStatisticsFileName;

            var stepStatus = await _statusService.GetStepStatusAsync(
                request.AnalysisId,
                stepName,
                cancellationToken);

            if (stepStatus.Status == AnalysisStepStatus.Failed)
            {
                return Result.Fail(new StepFailedError(
                    stepName,
                    stepStatus.ErrorMessage));
            }

            if (stepStatus.Status != AnalysisStepStatus.Completed)
            {
                return Result.Fail(new StepNotCompletedError(
                    stepName,
                    stepStatus.Status,
                    stepStatus.ErrorMessage));
            }

            var statisticsDto = await _storageService.ReadDataAsync<AssemblyStatisticsDto>(
                request.AnalysisId,
                fileName,
                cancellationToken);

            if (statisticsDto == null)
            {
                _logger.LogError(
                    "Statistics file missing for completed analysis {AnalysisId}",
                    request.AnalysisId);

                return Result.Fail(new Error(
                    "Internal error: Statistics step completed but file is missing."));
            }

            return Result.Ok(statisticsDto);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Statistics requested for non-existent analysis: {AnalysisId}",
                ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}
