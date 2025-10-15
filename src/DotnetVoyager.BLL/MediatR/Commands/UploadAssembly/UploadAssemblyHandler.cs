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

public class UploadAssemblyHandler : IRequestHandler<UploadAssemblyCommand, Result<UploadAssemblyResultDto>>
{
    private readonly IStorageService _storageService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly ILogger<UploadAssemblyHandler> _logger;
    private readonly IValidator<UploadAssemblyDto> _uploadRequestValidator;

    public UploadAssemblyHandler(
        IStorageService storageService,
        IBackgroundTaskQueue backgroundTaskQueue,
        IValidator<UploadAssemblyDto> uploadRequestValidator,
        ILogger<UploadAssemblyHandler> logger,
        IDependencyAnalyzerService dependencyAnalyzer)
    {
        _logger = logger;
        _storageService = storageService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _uploadRequestValidator = uploadRequestValidator;
    }

    public async Task<Result<UploadAssemblyResultDto>> Handle(UploadAssemblyCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _uploadRequestValidator.ValidateAsync(request.uploadDto);

        if (!validationResult.IsValid)
        {
            return Result.Fail(new ValidationError(validationResult));
        }

        var file = request.uploadDto.File!;
        var analysisId = Guid.NewGuid().ToString();

        await _storageService.SaveAssemblyFileAsync(file, analysisId);
        _logger.LogInformation("Saved uploaded file '{FileName}' for analysis ID '{AnalysisId}'", file.FileName, analysisId);

        var analysisTask = new AnalysisTask(analysisId);

        await _backgroundTaskQueue.EnqueueAsync(analysisTask);
        _logger.LogInformation("Enqueued analysis task to background worker for ID '{AnalysisId}'", analysisId);

        var response = new UploadAssemblyResultDto
        {
            AnalysisId = analysisId
        };

        return Result.Ok(response);
    }
}
