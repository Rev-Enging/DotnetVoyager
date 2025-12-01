using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Workers;
using FluentResults;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Commands.UploadAssembly;

public record UploadAssemblyCommand(UploadAssemblyDto uploadDto) : IRequest<Result<UploadAssemblyResultDto>>;

public class UploadAssemblyHandler : IRequestHandler<UploadAssemblyCommand, Result<UploadAssemblyResultDto>>
{
    private readonly IStorageService _storageService;
    private readonly IAnalysisTaskQueue _taskQueue;
    private readonly ILogger<UploadAssemblyHandler> _logger;
    private readonly IValidator<UploadAssemblyDto> _validator;
    private readonly IAnalysisStatusService _statusService;

    public UploadAssemblyHandler(
        IStorageService storageService,
        IAnalysisTaskQueue taskQueue,
        IValidator<UploadAssemblyDto> validator,
        ILogger<UploadAssemblyHandler> logger,
        IAnalysisStatusService statusService)
    {
        _storageService = storageService;
        _taskQueue = taskQueue;
        _validator = validator;
        _logger = logger;
        _statusService = statusService;
    }

    public async Task<Result<UploadAssemblyResultDto>> Handle(
        UploadAssemblyCommand request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(
            request.uploadDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.Fail(new ValidationError(validationResult));
        }

        var file = request.uploadDto.File!;
        var analysisId = Guid.NewGuid().ToString();

        // 1. Create analysis record and initialize steps
        await _statusService.CreateAnalysisAsync(analysisId, file.FileName, cancellationToken);
        _logger.LogInformation(
            "Created analysis record with steps for ID '{AnalysisId}'", analysisId);

        // 2. Save file
        await _storageService.SaveAssemblyFileAsync(file, analysisId, cancellationToken);
        _logger.LogInformation(
            "Saved file '{FileName}' for analysis '{AnalysisId}'",
            file.FileName, analysisId);

        // 3. Enqueue for processing
        await _taskQueue.EnqueueAsync(new AnalysisTask(analysisId));
        _logger.LogInformation(
            "Enqueued analysis task for ID '{AnalysisId}'", analysisId);

        return Result.Ok(new UploadAssemblyResultDto { AnalysisId = analysisId });
    }
}
