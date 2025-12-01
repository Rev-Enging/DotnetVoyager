using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Models;
using DotnetVoyager.BLL.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Text.Json;

namespace DotnetVoyager.BLL.Services;

public interface IStorageService
{
    string GetAnalysisDirectoryPath(string analysisId);
    Task<AnalysisLocationContext?> CreateAnalysisContextAsync(string analysisId, CancellationToken token = default);
    Task<string?> FindAssemblyFilePathAsync(string analysisId, CancellationToken token = default);
    Task<string> SaveAssemblyFileAsync(FileDto file, string analysisId, CancellationToken token = default);
    Task DeleteAnalysisAsync(string analysisId, CancellationToken token = default);
    Task SaveDataAsync(string analysisId, object data, string fileName, CancellationToken token = default);
    Task<T?> ReadDataAsync<T>(string analysisId, string fileName, CancellationToken token = default) where T : class;
}

public class StorageService : IStorageService
{
    private const string DllExtension = ".dll";
    private const string ExeExtension = ".exe";

    private readonly TimeSpan _pauseBetweenFailures = TimeSpan.FromMilliseconds(50);
    private readonly int _maxRetryAttempts = 3;

    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly string _basePath;
    private readonly IDecompilerCacheService _decompilerCache;
    private readonly ILogger<StorageService> _logger;

    public StorageService(
        IOptions<StorageOptions> options,
        IDecompilerCacheService decompilerCache,
        ILogger<StorageService> logger)
    {
        _basePath = options.Value.Path;
        _decompilerCache = decompilerCache;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<IOException>()
            .WaitAndRetryAsync(_maxRetryAttempts, retryAttempt => _pauseBetweenFailures);
    }

    public async Task<AnalysisLocationContext?> CreateAnalysisContextAsync(string analysisId, CancellationToken token = default)
    {
        var assemblyPath = await FindAssemblyFilePathAsync(analysisId, token);
        if (assemblyPath == null) return null;

        return new AnalysisLocationContext
        {
            AnalysisId = analysisId,
            AssemblyPath = assemblyPath,
            AnalysisDirectory = GetAnalysisDirectoryPath(analysisId)
        };
    }

    public string GetAnalysisDirectoryPath(string analysisId)
    {
        return Path.Combine(_basePath, analysisId);
    }

    public Task<string?> FindAssemblyFilePathAsync(string analysisId, CancellationToken token = default)
    {
        var directoryPath = GetAnalysisDirectoryPath(analysisId);
        if (!Directory.Exists(directoryPath))
        {
            return Task.FromResult<string?>(null);
        }

        var assemblyFile = Directory.EnumerateFiles(directoryPath)
            .FirstOrDefault(f => f.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase) ||
                                 f.EndsWith(ExeExtension, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(assemblyFile);
    }

    public async Task DeleteAnalysisAsync(string analysisId, CancellationToken token = default)
    {
        var assemblyPath = await FindAssemblyFilePathAsync(analysisId, token);
        if (assemblyPath != null)
        {
            _decompilerCache.RemovePEFile(assemblyPath);
            _logger.LogDebug("Cleared decompiler cache for: {Path}", assemblyPath);
        }

        var directoryPath = GetAnalysisDirectoryPath(analysisId);
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
            _logger.LogInformation("Deleted analysis directory: {AnalysisId}", analysisId);
        }
    }

    public async Task<string> SaveAssemblyFileAsync(FileDto file, string analysisId, CancellationToken token = default)
    {
        var targetDirectoryPath = GetAnalysisDirectoryPath(analysisId);
        Directory.CreateDirectory(targetDirectoryPath);

        var filePath = Path.Combine(targetDirectoryPath, file.FileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.FileStream.CopyToAsync(stream, token);
        }

        _logger.LogInformation("Saved assembly file: {FileName} for {AnalysisId}", file.FileName, analysisId);
        return file.FileName;
    }

    public async Task SaveDataAsync(string analysisId, object data, string fileName, CancellationToken token = default)
    {
        var targetDirectoryPath = GetAnalysisDirectoryPath(analysisId);
        var filePath = Path.Combine(targetDirectoryPath, fileName);

        await _retryPolicy.ExecuteAsync(async (CancellationToken ct) =>
        {
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(stream, data, new JsonSerializerOptions { WriteIndented = true }, ct);
            }
        }, token);
    }

    public async Task<T?> ReadDataAsync<T>(string analysisId, string fileName, CancellationToken token = default) where T : class
    {
        var targetDirectoryPath = GetAnalysisDirectoryPath(analysisId);
        var filePath = Path.Combine(targetDirectoryPath, fileName);

        if (!File.Exists(filePath))
        {
            return null;
        }

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<T>(stream);
        });
    }
}
