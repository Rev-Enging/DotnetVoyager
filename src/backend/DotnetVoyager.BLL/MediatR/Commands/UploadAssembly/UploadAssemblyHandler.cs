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
    private readonly IAnalysisTaskQueue _backgroundTaskQueue;
    private readonly ILogger<UploadAssemblyHandler> _logger;
    private readonly IValidator<UploadAssemblyDto> _uploadRequestValidator;
    private readonly IAnalysisStatusService _analysisStatusService;

    public UploadAssemblyHandler(
        IStorageService storageService,
        IAnalysisTaskQueue backgroundTaskQueue,
        IValidator<UploadAssemblyDto> uploadRequestValidator,
        ILogger<UploadAssemblyHandler> logger,
        IAnalysisStatusService analysisStatusService)
    {
        _logger = logger;
        _storageService = storageService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _uploadRequestValidator = uploadRequestValidator;
        _analysisStatusService = analysisStatusService;
    }

    public async Task<Result<UploadAssemblyResultDto>> Handle(UploadAssemblyCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _uploadRequestValidator.ValidateAsync(request.uploadDto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Result.Fail(new ValidationError(validationResult));
        }

        var file = request.uploadDto.File!;
        var analysisId = Guid.NewGuid().ToString();

        await _analysisStatusService.CreateJobAsync(analysisId, file.FileName, cancellationToken);
        _logger.LogInformation("Created initial DB job record for ID '{AnalysisId}' with file name '{FileName}'", analysisId, file.FileName);

        await _storageService.SaveAssemblyFileAsync(file, analysisId, cancellationToken);
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
