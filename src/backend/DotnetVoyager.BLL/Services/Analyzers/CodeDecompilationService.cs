using DotnetVoyager.BLL.Dtos.AnalysisResults;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface ICodeDecompilationService
{
    Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token);
}

public class CodeDecompilationService : ICodeDecompilationService
{
    private readonly IMemoryCache _cache;
    private readonly IDecompilerCacheService _decompilerCache;
    private readonly ILogger<CodeDecompilationService> _logger;

    public CodeDecompilationService(
        IMemoryCache cache,
        IDecompilerCacheService decompilerCache,
        ILogger<CodeDecompilationService> logger)
    {
        _cache = cache;
        _decompilerCache = decompilerCache;
        _logger = logger;
    }

    public Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token)
    {
        // Generate cache key
        var cacheKey = $"decompiled_{assemblyPath}_{token}";

        // Try get from cache
        if (_cache.TryGetValue<DecompiledCodeDto>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache HIT for {Path}:{Token}", assemblyPath, token);
            return Task.FromResult(cached!);
        }

        _logger.LogDebug("Cache MISS for {Path}:{Token}", assemblyPath, token);

        // Perform decompilation
        var result = PerformDecompilation(assemblyPath, token);

        // Store in cache (30 minutes)
        var cacheOptions = new MemoryCacheEntryOptions
        {
            Size = 1,
            Priority = CacheItemPriority.Normal,
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        _cache.Set(cacheKey, result, cacheOptions);

        return Task.FromResult(result);
    }

    private DecompiledCodeDto PerformDecompilation(string assemblyPath, int token)
    {
        // Get or create cached PEFile
        var peFile = _decompilerCache.GetOrCreatePEFile(assemblyPath);

        if (peFile == null)
        {
            throw new InvalidOperationException($"Failed to load assembly: {assemblyPath}");
        }

        try
        {
            // Setup Decompiler
            var resolver = new UniversalAssemblyResolver(
                assemblyPath,
                false,
                peFile.DetectTargetFrameworkId());

            var decompiler = new CSharpDecompiler(peFile, resolver, new DecompilerSettings());

            // Decompile C#
            var handle = MetadataTokens.EntityHandle(token);
            var csharpCode = decompiler.DecompileAsString(handle);

            // Disassemble IL
            string ilCode = GetIlCode(peFile, handle);

            return new DecompiledCodeDto
            {
                CSharpCode = csharpCode,
                IlCode = ilCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decompilation failed for {Path}:{Token}", assemblyPath, token);
            throw;
        }
    }

    private static string GetIlCode(ICSharpCode.Decompiler.Metadata.PEFile peFile, EntityHandle handle)
    {
        if (handle.Kind != HandleKind.MethodDefinition)
        {
            return "IL code is available only for methods.";
        }

        var output = new PlainTextOutput();
        var disassembler = new ReflectionDisassembler(output, CancellationToken.None);
        disassembler.DisassembleMethod(peFile, (MethodDefinitionHandle)handle);

        return output.ToString();
    }
}