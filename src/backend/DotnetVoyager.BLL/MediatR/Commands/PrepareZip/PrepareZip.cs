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
    private readonly IAnalysisStatusService _statusService;
    private readonly IDecompilationTaskQueue _taskQueue;
    private readonly ILogger<PrepareZipHandler> _logger;

    public PrepareZipHandler(
        IAnalysisStatusService statusService,
        IDecompilationTaskQueue taskQueue,
        ILogger<PrepareZipHandler> logger)
    {
        _statusService = statusService;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    public async Task<Result> Handle(PrepareZipCommand request, CancellationToken cancellationToken)
    {
        bool weStartedTheJob = await _statusService.TryStartZipGenerationAsync(
            request.AnalysisId,
            cancellationToken);

        if (weStartedTheJob)
        {
            var task = new AnalysisTask(request.AnalysisId);
            await _taskQueue.EnqueueAsync(task);
            _logger.LogInformation("Enqueued full decompilation task for {AnalysisId}", request.AnalysisId);
        }
        else
        {
            _logger.LogInformation("Zip generation for {AnalysisId} is already in progress or not ready to start.", request.AnalysisId);
        }

        return Result.Ok();
    }
}
