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
        var cacheKey = $"decompiled_{assemblyPath}_{token}";

        // Check if we have already decompiled this entity to save CPU.
        if (_cache.TryGetValue<DecompiledCodeDto>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache HIT for {Path}:{Token}", assemblyPath, token);
            return Task.FromResult(cached!);
        }

        _logger.LogDebug("Cache MISS for {Path}:{Token}", assemblyPath, token);

        var result = PerformDecompilation(assemblyPath, token);

        // Cache the result for 30 minutes.
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
        // Reuse the opened PEFile instance to avoid disk I/O.
        var peFile = _decompilerCache.GetOrCreatePEFile(assemblyPath);

        if (peFile == null)
        {
            throw new InvalidOperationException($"Failed to load assembly: {assemblyPath}");
        }

        try
        {
            // Resolver is needed to find types from other DLLs (e.g. System.String).
            var resolver = new UniversalAssemblyResolver(
                assemblyPath,
                false,
                peFile.DetectTargetFrameworkId());

            var decompiler = new CSharpDecompiler(peFile, resolver, new DecompilerSettings());

            // Convert integer token (ID) back to a Handle struct required by the library.
            var handle = MetadataTokens.EntityHandle(token);

            // Reconstruct high-level C# code (loops, async/await, etc.).
            var csharpCode = decompiler.DecompileAsString(handle);

            // Extract low-level IL instructions.
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

    private static string GetIlCode(PEFile peFile, EntityHandle handle)
    {
        // IL code exists only for methods. Classes or properties don't have direct IL instructions.
        if (handle.Kind != HandleKind.MethodDefinition)
        {
            return "IL code is available only for methods.";
        }

        var output = new PlainTextOutput();

        // Disassembler translates raw bytes into readable IL text (e.g., "ldstr", "call").
        var disassembler = new ReflectionDisassembler(output, CancellationToken.None);
        disassembler.DisassembleMethod(peFile, (MethodDefinitionHandle)handle);

        return output.ToString();
    }
}