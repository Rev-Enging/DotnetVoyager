using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Workers;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Commands.PrepareZip;

public record PrepareZipCommand(string AnalysisId) : IRequest<Result>;

public class PrepareZipHandler : IRequestHandler<PrepareZipCommand, Result>
{
    private readonly IAnalysisStepService _stepService;
    private readonly IAnalysisTaskQueue _taskQueue;
    private readonly IAnalysisStatusService _statusService;
    private readonly ILogger<PrepareZipHandler> _logger;

    public PrepareZipHandler(
        IAnalysisStepService stepService,
        IAnalysisTaskQueue taskQueue,
        ILogger<PrepareZipHandler> logger,
        IAnalysisStatusService statusService)
    {
        _stepService = stepService;
        _taskQueue = taskQueue;
        _logger = logger;
        _statusService = statusService;
    }

    public async Task<Result> Handle(
        PrepareZipCommand request,
        CancellationToken cancellationToken)
    {
        var statusDto = await _statusService.GetStatusAsync(
            request.AnalysisId,
            cancellationToken);

        if (statusDto == null)
        {
            return Result.Fail(new NotFoundError(
                $"Analysis with ID '{request.AnalysisId}' not found."));
        }

        bool added = await _stepService.TryAddOptionalStepAsync(
            request.AnalysisId,
            AnalysisStepNames.ZipGeneration,
            cancellationToken);

        if (added)
        {
            await _taskQueue.EnqueueAsync(new AnalysisTask(request.AnalysisId));
            _logger.LogInformation(
                "ZipGeneration step added and task enqueued for {AnalysisId}",
                request.AnalysisId);
        }
        else
        {
            _logger.LogInformation(
                "ZipGeneration step already exists for {AnalysisId}",
                request.AnalysisId);
        }

        return Result.Ok();
    }
}
