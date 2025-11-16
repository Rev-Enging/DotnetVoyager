using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Errors;
using DotnetVoyager.BLL.Exceptions;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.DAL.Enums;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetFullDecompiledCodeInZip;

public record FullDecompiledCodeInZipDto(
    Stream FileStream,
    string ContentType,
    string FileDownloadName);

public record DownloadZipQuery(string AnalysisId) : IRequest<Result<FullDecompiledCodeInZipDto>>;

public class DownloadZipHandler : IRequestHandler<DownloadZipQuery, Result<FullDecompiledCodeInZipDto>>
{
    private readonly IAnalysisStatusService _statusService;
    private readonly IStorageService _storageService;
    private readonly ILogger<DownloadZipHandler> _logger;

    public DownloadZipHandler(
        IAnalysisStatusService statusService, 
        IStorageService storageService, 
        ILogger<DownloadZipHandler> logger)
    {
        _statusService = statusService;
        _storageService = storageService;
        _logger = logger;
    }


    public async Task<Result<FullDecompiledCodeInZipDto>> Handle(
        DownloadZipQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            const string stepName = AnalysisStepNames.ZipGeneration;
            const string zipFileName = ProjectConstants.DecompiledZipFileName;

            var stepStatus = await _statusService.GetStepStatusAsync(
                request.AnalysisId,
                stepName,
                cancellationToken);

            if (stepStatus.Status == AnalysisStepStatus.NotProcessed)
            {
                return Result.Fail(new StepNotProcessedError(
                    request.AnalysisId,
                    stepName));
            }

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

            var analysisFolder = _storageService.GetAnalysisDirectoryPath(request.AnalysisId);
            var zipPath = Path.Combine(analysisFolder, zipFileName);

            if (!File.Exists(zipPath))
            {
                _logger.LogError(
                    "ZIP file marked as completed but not found at {Path} for {AnalysisId}",
                    zipPath, request.AnalysisId);

                return Result.Fail(new Error(
                    "Internal error: ZIP generation completed but file is missing."));
            }

            var stream = new FileStream(
                zipPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var resultDto = new FullDecompiledCodeInZipDto(
                FileStream: stream,
                ContentType: "application/zip",
                FileDownloadName: $"decompiled_{request.AnalysisId}.zip"
            );

            return Result.Ok(resultDto);
        }
        catch (AnalysisNotFoundException ex)
        {
            _logger.LogWarning(
                ex,
                "Download ZIP requested for non-existent analysis: {AnalysisId}",
                ex.AnalysisId);

            return Result.Fail(new AnalysisNotFound(ex.AnalysisId));
        }
        catch (IOException ex)
        {
            _logger.LogError(
                ex,
                "Failed to open ZIP file stream for {AnalysisId}",
                request.AnalysisId);

            return Result.Fail(new Error("Failed to open ZIP file stream."));
        }
    }
}
