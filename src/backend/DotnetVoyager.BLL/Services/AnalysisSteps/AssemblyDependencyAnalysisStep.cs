using DotnetVoyager.BLL.Constants;
using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Models;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace DotnetVoyager.BLL.Services.AnalysisSteps;

public class AssemblyDependencyAnalysisStep : IAnalysisStep
{
    private readonly IStorageService _storageService;
    private readonly IAssemblyReferenceAnalyzer _assemblyReferenceService;
    private readonly ILogger<AssemblyDependencyAnalysisStep> _logger;

    public string StepName => AnalysisStepNames.AssemblyDependencies;

    public AssemblyDependencyAnalysisStep(
        IAssemblyReferenceAnalyzer assemblyReferenceService,
        IStorageService storageService,
        ILogger<AssemblyDependencyAnalysisStep> logger)
    {
        _assemblyReferenceService = assemblyReferenceService;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<bool> ExecuteAsync(AnalysisLocationContext analysisLocationContext, CancellationToken cancellationToken)
    {
        var analysisId = analysisLocationContext.AnalysisId;
        var assemblyPath = analysisLocationContext.AssemblyPath;

        try
        {
            _logger.LogInformation("Executing {Step} for {AnalysisId}", StepName, analysisId);

            var assemblyDependencies = await _assemblyReferenceService.AnalyzeReferences(assemblyPath);

            await _storageService.SaveDataAsync(
                analysisId,
                assemblyDependencies,
                ProjectConstants.AssemblyDependenciesFileName,
                cancellationToken);

            _logger.LogInformation("Completed {Step} for {AnalysisId}", StepName, analysisId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {Step} for {AnalysisId}", StepName, analysisId);
            return false;
        }
    }
}

/// <summary>
/// Analyzes assembly dependencies and extracts metadata about referenced assemblies
/// using low-level System.Reflection.Metadata API for performance.
/// </summary>
public interface IAssemblyReferenceAnalyzer
{
    Task<AssemblyDependenciesDto> AnalyzeReferences(string assemblyPath);
}

/// <summary>
/// Implementation that reads assembly metadata to build a dependency graph
/// with optimized memory usage through stackalloc and Span&lt;T&gt;.
/// </summary>
public class AssemblyReferenceAnalyzer : IAssemblyReferenceAnalyzer
{
    public Task<AssemblyDependenciesDto> AnalyzeReferences(string assemblyPath)
    {
        return Task.Run(() =>
        {
            using var fileStream = new FileStream(
                assemblyPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read | FileShare.Delete);

            using var peReader = new PEReader(fileStream);

            // If metadata is missing, the file is invalid for analysis
            // The caller (AnalysisWorker) will catch this and mark the step as "Failed"
            if (!peReader.HasMetadata)
            {
                throw new BadImageFormatException("The provided file does not contain CLI metadata.");
            }

            var metadataReader = peReader.GetMetadataReader();
            var assemblyDef = metadataReader.GetAssemblyDefinition();

            var refsCount = metadataReader.AssemblyReferences.Count;
            var references = new List<AssemblyReferenceDto>(refsCount);

            var pkBlob = metadataReader.GetBlobBytes(assemblyDef.PublicKey);

            // Construct the root assembly node with all required metadata
            var graph = new AssemblyDependenciesDto
            {
                AssemblyName = metadataReader.GetString(assemblyDef.Name),
                Version = assemblyDef.Version.ToString(),
                Culture = GetCultureString(metadataReader, assemblyDef.Culture),
                PublicKeyToken = GetPublicKeyTokenFromBlob(pkBlob),
                References = references
            };

            // Process all assembly references (dependencies)
            foreach (var assemblyRefHandle in metadataReader.AssemblyReferences)
            {
                var assemblyRef = metadataReader.GetAssemblyReference(assemblyRefHandle);
                var refPkBlob = metadataReader.GetBlobBytes(assemblyRef.PublicKeyOrToken);

                references.Add(new AssemblyReferenceDto
                {
                    Name = metadataReader.GetString(assemblyRef.Name),
                    Version = assemblyRef.Version.ToString(),
                    Culture = GetCultureString(metadataReader, assemblyRef.Culture),
                    PublicKeyToken = GetPublicKeyTokenFromBlob(refPkBlob)
                });
            }

            return graph;
        });
    }

    // Extracts culture info from metadata, defaulting to "neutral" for invariant culture
    private static string GetCultureString(MetadataReader reader, StringHandle handle)
    {
        if (handle.IsNil) return "neutral";
        var culture = reader.GetString(handle);
        return string.IsNullOrEmpty(culture) ? "neutral" : culture;
    }

    // Converts a Public Key (full) or Public Key Token (short) into a hex string.
    // Uses Span and stackalloc to avoid heap allocations for better performance.
    // Public Key Token is used for strong-name assembly identification.
    private static string GetPublicKeyTokenFromBlob(byte[] publicKeyData)
    {
        if (publicKeyData == null || publicKeyData.Length == 0)
            return "null";

        // Case 1: Already a Token (8 bytes)
        // Usually found in Assembly References (PublicKeyOrToken field)
        if (publicKeyData.Length == 8)
        {
            return ToHexString(publicKeyData);
        }

        // Case 2: Full Public Key
        // Usually found in Assembly Definition. Must be hashed to get the Token.
        // Algorithm: SHA1 hash -> Take last 8 bytes -> Reverse byte order -> Convert to hex

        // Use static SHA1.HashData (.NET 6+) to avoid allocating SHA1 object instance
        Span<byte> hash = stackalloc byte[20]; // SHA1 is always 20 bytes
        SHA1.HashData(publicKeyData, hash);

        // Token is the last 8 bytes of the SHA1 hash, reversed
        Span<byte> tokenSpan = hash.Slice(hash.Length - 8);
        tokenSpan.Reverse();

        return ToHexString(tokenSpan);
    }

    // Fast hex conversion without BitConverter allocations
    // Uses string.Create for zero-allocation string building
    private static string ToHexString(ReadOnlySpan<byte> bytes)
    {
        return string.Create(bytes.Length * 2, bytes.ToArray(), (chars, buf) =>
        {
            var i = 0;
            foreach (var b in buf)
            {
                chars[i++] = ToHexChar(b >> 4);
                chars[i++] = ToHexChar(b & 0xF);
            }
        });
    }

    // Converts a nibble (4 bits) to its hex character representation
    private static char ToHexChar(int n)
    {
        return (char)(n < 10 ? n + '0' : n - 10 + 'a');
    }
}
