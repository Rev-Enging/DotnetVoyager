using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Enums;

namespace DotnetVoyager.BLL.Services;

public interface IAnalysisStatusService
{
    /// <summary>
    /// Creates or updates the status file for a given analysis.
    /// </summary>
    Task SetStatusAsync(string analysisId, AnalysisStatus status, string? errorMessage = null, CancellationToken token = default);

    /// <summary>
    /// Reads the current status file for a given analysis.
    /// </summary>
    Task<AnalysisStatusDto?> GetStatusAsync(string analysisId, CancellationToken token = default);
}

public class AnalysisStatusService : IAnalysisStatusService
{
    private readonly IStorageService _storageService;

    public AnalysisStatusService(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public Task SetStatusAsync(string analysisId, AnalysisStatus status, string? errorMessage = null, CancellationToken token = default)
    {
        var statusDto = new AnalysisStatusDto
        {
            AnalysisId = analysisId,
            Status = status,
            ErrorMessage = errorMessage,
            LastUpdatedUtc = DateTime.UtcNow
        };

        // Use the constant you defined
        return _storageService.SaveDataAsync(analysisId, statusDto, ProjectConstants.AnalysisStatusFileName, token);
    }

    public Task<AnalysisStatusDto?> GetStatusAsync(string analysisId, CancellationToken token = default)
    {
        // Use the constant you defined
        return _storageService.ReadDataAsync<AnalysisStatusDto>(analysisId, ProjectConstants.AnalysisStatusFileName, token);
    }
}
