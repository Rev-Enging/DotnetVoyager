using ICSharpCode.Decompiler.Metadata;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace DotnetVoyager.BLL.Services;

public interface IDecompilerCacheService : IDisposable
{
    PEFile? GetOrCreatePEFile(string assemblyPath);
    void RemovePEFile(string assemblyPath);
}

public class DecompilerCacheService : IDecompilerCacheService
{
    private readonly ConcurrentDictionary<string, PEFile> _peFileCache = new();
    private readonly ILogger<DecompilerCacheService> _logger;

    public DecompilerCacheService(ILogger<DecompilerCacheService> logger)
    {
        _logger = logger;
    }

    public PEFile? GetOrCreatePEFile(string assemblyPath)
    {
        return _peFileCache.GetOrAdd(assemblyPath, path =>
        {
            try
            {
                var stream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read | FileShare.Delete);

                var peFile = new PEFile(path, stream);

                _logger.LogDebug("Created and cached PEFile for: {Path}", path);
                return peFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create PEFile for: {Path}", path);
                return null!;
            }
        });
    }

    public void RemovePEFile(string assemblyPath)
    {
        if (_peFileCache.TryRemove(assemblyPath, out var peFile))
        {
            try
            {
                peFile.Dispose();
                _logger.LogDebug("Removed and disposed PEFile for: {Path}", assemblyPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing PEFile for: {Path}", assemblyPath);
            }
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _peFileCache)
        {
            try
            {
                kvp.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing PEFile during cleanup: {Path}", kvp.Key);
            }
        }
        _peFileCache.Clear();
        _logger.LogInformation("DecompilerCacheService disposed");
    }
}