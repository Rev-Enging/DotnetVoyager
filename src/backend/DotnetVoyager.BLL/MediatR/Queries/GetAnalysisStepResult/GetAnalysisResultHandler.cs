using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.Common;

public record GetAnalysisResultQuery<TResult>(
    string AnalysisId,
    string StepName,
    string FileName) : IRequest<Result<TResult>> where TResult : class;

public class GetAnalysisResultHandler<TResult>
    : IRequestHandler<GetAnalysisResultQuery<TResult>, Result<TResult>>
    where TResult : class
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<GetAnalysisResultHandler<TResult>> _logger;

    public GetAnalysisResultHandler(
        IStorageService storageService,
        IAnalysisStatusService statusService,
        ILogger<GetAnalysisResultHandler<TResult>> logger)
    {
        _storageService = storageService;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<Result<TResult>> Handle(
        GetAnalysisResultQuery<TResult> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stepStatus = await _statusService.GetStepStatusAsync(
                request.AnalysisId,
                request.StepName,
                cancellationToken);

            if (stepStatus.Status == AnalysisStepStatus.Failed)
            {
                return Result.Fail(new StepFailedError(
                    request.StepName,
                    stepStatus.ErrorMessage));
            }

            if (stepStatus.Status != AnalysisStepStatus.Completed)
            {
                return Result.Fail(new StepNotCompletedError(
                    request.StepName,
                    stepStatus.Status,
                    stepStatus.ErrorMessage));
            }

            var result = await _storageService.ReadDataAsync<TResult>(
                request.AnalysisId,
                request.FileName,
                cancellationToken);

            if (result == null)
            {
                _logger.LogError(
                    "{StepName} file missing for completed analysis {AnalysisId}",
                    request.StepName, request.AnalysisId);

                return Result.Fail(new Error(
                    $"Internal error: {request.StepName} step completed but file is missing."));
            }

            return Result.Ok(result);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "{StepName} requested for non-existent analysis: {AnalysisId}",
                request.StepName, ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
    }
}