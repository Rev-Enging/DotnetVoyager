using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.BLL.Services;
using DotnetVoyager.BLL.Utils;
using FluentResults;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.MediatR.Queries.GetFullDecompiledCodeInZip;

public record FullDecompiledCodeInZipDto(
    Stream FileStream,
    string ContentType,
    string FileDownloadName);

public record DownloadZipCommand(string AnalysisId) : IRequest<Result<FullDecompiledCodeInZipDto>>;

public class DownloadZipHandler : IRequestHandler<DownloadZipCommand, Result<FullDecompiledCodeInZipDto>>
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

    public async Task<Result<FullDecompiledCodeInZipDto>> Handle(DownloadZipCommand request, CancellationToken cancellationToken)
    {
        var statusDto = await _statusService.GetStatusAsync(request.AnalysisId, cancellationToken);
        
        if (statusDto == null)
        {
            return Results.NotFound($"Analysis not found for ID: {request.AnalysisId}.");
        }

        if (statusDto.ZipStatus != ZipGenerationStatus.Completed)
        {
            // Check for specific failure case
            if (statusDto.ZipStatus == ZipGenerationStatus.Failed)
            {
                return Result.Fail($"ZIP generation failed. Error: {statusDto.ZipErrorMessage}");
            }

            // General "Not Ready" failure
            string message = $"ZIP generation status is {statusDto.ZipStatus}. It must be Completed.";
            return Result.Fail(message);
        }

        // 2. Determine file path
        var analysisFolder = _storageService.GetAnalysisDirectoryPath(request.AnalysisId);
        var zipPath = Path.Combine(analysisFolder, ProjectConstants.DecompiledZipFileName);

        // 3. Final check of file existence
        if (!File.Exists(zipPath))
        {
            _logger.LogWarning("Zip file marked as 'Completed' in DB but not found on disk at {Path}. Data inconsistency suspected.", zipPath);
            return Result.Fail("File not found on disk. Data inconsistency or cleanup issue.");
        }

        try
        {
            var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            var resultDto = new FullDecompiledCodeInZipDto(
                FileStream: stream,
                ContentType: "application/zip",
                FileDownloadName: $"decompiled_{request.AnalysisId}.zip"
            );

            return Result.Ok(resultDto);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to open ZIP file stream for {AnalysisId}", request.AnalysisId);
            return Result.Fail("Error opening the file stream on server.");
        }
    }
}
