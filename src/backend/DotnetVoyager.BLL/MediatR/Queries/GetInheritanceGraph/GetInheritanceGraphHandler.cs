using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetInheritanceGraph;

public record GetInheritanceGraphQuery(string AnalysisId) : IRequest<Result<InheritanceGraphDto>>;

public class GetInheritanceGraphHandler : IRequestHandler<GetInheritanceGraphQuery, Result<InheritanceGraphDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<GetInheritanceGraphHandler> _logger;

    public GetInheritanceGraphHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetInheritanceGraphHandler> logger)
    {
        _storageService = storageService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Result<InheritanceGraphDto>> Handle(
        GetInheritanceGraphQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string stepName = AnalysisStepNames.InheritanceGraph;
            const string fileName = ProjectConstants.AnalysisInheritanceGraphFileName;

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

            var graphDto = await _storageService.ReadDataAsync<InheritanceGraphDto>(
                request.AnalysisId,
                fileName,
                cancellationToken);

            if (graphDto == null)
            {
                _logger.LogError(
                    "Inheritance graph file missing for completed analysis {AnalysisId}",
                    request.AnalysisId);

                return Result.Fail(new Error(
                    "Internal error: Inheritance graph step completed but file is missing."));
            }

            return Result.Ok(graphDto);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Inheritance graph requested for non-existent analysis: {AnalysisId}",
                ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}
