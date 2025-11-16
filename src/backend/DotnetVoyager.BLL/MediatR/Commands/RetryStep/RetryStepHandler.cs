using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Workers;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Commands.RetryStep;

public record RetryStepCommand(string AnalysisId, string StepName) : IRequest<Result>;

public class RetryStepHandler : IRequestHandler<RetryStepCommand, Result>
{
    private readonly IAnalysisStatusService _statusService;
    private readonly IAnalysisStepService _stepService;
    private readonly IAnalysisTaskQueue _taskQueue;
    private readonly ILogger<RetryStepHandler> _logger;

    public RetryStepHandler(
        IAnalysisStatusService statusService,
        IAnalysisStepService stepService,
        IAnalysisTaskQueue taskQueue,
        ILogger<RetryStepHandler> logger)
    {
        _statusService = statusService;
        _stepService = stepService;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    public async Task<Result> Handle(
        RetryStepCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stepStatus = await _statusService.GetStepStatusAsync(
                request.AnalysisId,
                request.StepName,
                cancellationToken);

            if (stepStatus.Status == AnalysisStepStatus.NotProcessed)
            {
                return Result.Fail(new StepNotProcessedError(
                    request.AnalysisId,
                    request.StepName));
            }

            if (stepStatus.Status != AnalysisStepStatus.Failed)
            {
                return Result.Fail(new Error(
                    $"Step '{request.StepName}' cannot be retried. " +
                    $"Current status: {stepStatus.Status}. " +
                    $"Only failed steps can be retried."));
            }

            bool reset = await _stepService.ResetStepForRetryAsync(
                request.AnalysisId,
                request.StepName,
                cancellationToken);

            if (!reset)
            {
                return Result.Fail(new Error(
                    $"Failed to reset step '{request.StepName}' for retry."));
            }

            await _taskQueue.EnqueueAsync(new AnalysisTask(request.AnalysisId));

            _logger.LogInformation(
                "Step {StepName} reset and task enqueued for {AnalysisId}",
                request.StepName, request.AnalysisId);

            return Result.Ok();
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Retry requested for step {StepName} in non-existent analysis: {AnalysisId}",
                request.StepName, ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
        catch (AnalysisStepNotExistException ex)
        {
            _logger.LogWarning(
                ex,
                "Retry requested for non-existent step {StepName} in analysis: {AnalysisId}",
                request.StepName, request.AnalysisId);
            return Result.Fail(new Error($"Step not exist: {request.StepName}"));
        }
    }
}