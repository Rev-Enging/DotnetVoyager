using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Enums;
using DotnetVoyager.DAL.Data;
using DotnetVoyager.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DotnetVoyager.BLL.Services;

public interface IAnalysisStatusService
{
    Task CreateJobAsync(string analysisId, string originalFileName, CancellationToken token = default);
    Task<bool> TryStartZipGenerationAsync(string analysisId, CancellationToken token = default);
    Task SetStatusAsync(string analysisId, AssemblyAnalysisStatus status, string? errorMessage = null, CancellationToken token = default);
    Task SetZipStatusAsync(string analysisId, ZipGenerationStatus zipStatus, string? errorMessage = null, CancellationToken token = default);
    Task<AnalysisStatusDto?> GetStatusAsync(string analysisId, CancellationToken token = default);
}

public class DatabaseAnalysisStatusService : IAnalysisStatusService
{
    private readonly AnalysisDbContext _db;
    private readonly ILogger<DatabaseAnalysisStatusService> _logger;

    public DatabaseAnalysisStatusService(AnalysisDbContext db, ILogger<DatabaseAnalysisStatusService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CreateJobAsync(string analysisId, string originalFileName, CancellationToken token = default)
    {
        var job = new AnalysisStatus
        {
            AnalysisId = analysisId,
            OriginalFileName = originalFileName,
            Status = AssemblyAnalysisStatus.Pending,
            ZipStatus = ZipGenerationStatus.NotStarted,
            CreatedUtc = DateTime.UtcNow,
            LastUpdatedUtc = DateTime.UtcNow
        };

        _db.AnalysisStatuses.Add(job);
        await _db.SaveChangesAsync(token);
    }

    public async Task<AnalysisStatusDto?> GetStatusAsync(string analysisId, CancellationToken token = default)
    {
        var job = await _db.AnalysisStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.AnalysisId == analysisId, token);

        if (job == null) return null;

        return new AnalysisStatusDto
        {
            AnalysisId = job.AnalysisId,
            Status = job.Status,
            ErrorMessage = job.ErrorMessage,
            ZipStatus = job.ZipStatus,
            ZipErrorMessage = job.ZipErrorMessage,
            LastUpdatedUtc = job.LastUpdatedUtc
        };
    }

    public async Task SetStatusAsync(string analysisId, AssemblyAnalysisStatus status, string? errorMessage = null, CancellationToken token = default)
    {
        await _db.AnalysisStatuses
            .Where(j => j.AnalysisId == analysisId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Status, status)
                .SetProperty(j => j.ErrorMessage, errorMessage)
                .SetProperty(j => j.LastUpdatedUtc, DateTime.UtcNow),
                cancellationToken: token);
    }

    public async Task SetZipStatusAsync(string analysisId, BLL.Enums.ZipGenerationStatus zipStatus, string? errorMessage = null, CancellationToken token = default)
    {
        await _db.AnalysisStatuses
            .Where(j => j.AnalysisId == analysisId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.ZipStatus, zipStatus)
                .SetProperty(j => j.ZipErrorMessage, errorMessage)
                .SetProperty(j => j.LastUpdatedUtc, DateTime.UtcNow),
                cancellationToken: token);
    }

    public async Task<bool> TryStartZipGenerationAsync(string analysisId, CancellationToken token = default)
    {
        var rowsAffected = await _db.AnalysisStatuses
            .Where(j => j.AnalysisId == analysisId &&
                        j.Status == AssemblyAnalysisStatus.Completed &&
                        j.ZipStatus == ZipGenerationStatus.NotStarted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.ZipStatus, ZipGenerationStatus.Pending)
                .SetProperty(j => j.ZipErrorMessage, (string?)null)
                .SetProperty(j => j.LastUpdatedUtc, DateTime.UtcNow),
                cancellationToken: token);

        return (rowsAffected > 0);
    }
}