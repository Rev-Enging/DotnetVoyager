using DotnetVoyager.BLL.Dtos.AnalysisResults;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IAssemblyReferenceAnalyzer
{
    Task<AssemblyDependenciesDto> AnalyzeReferences(string assemblyPath);
}

public class AssemblyReferenceAnalyzer : IAssemblyReferenceAnalyzer
{
    public Task<AssemblyDependenciesDto> AnalyzeReferences(string assemblyPath)
    {
        using var fileStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var peReader = new PEReader(fileStream);

        var metadataReader = peReader.GetMetadataReader();

        var assemblyDef = metadataReader.GetAssemblyDefinition();
        var assemblyName = metadataReader.GetString(assemblyDef.Name);
        var version = assemblyDef.Version;
        var culture = metadataReader.GetString(assemblyDef.Culture);

        var graph = new AssemblyDependenciesDto
        {
            AssemblyName = assemblyName,
            Version = version.ToString(),
            Culture = string.IsNullOrEmpty(culture) ? "neutral" : culture,
            PublicKeyToken = GetPublicKeyTokenFromBlob(metadataReader.GetBlobBytes(assemblyDef.PublicKey)),
            References = new List<AssemblyReferenceDto>()
        };

        // Get all dependencies
        foreach (var assemblyRefHandle in metadataReader.AssemblyReferences)
        {
            var assemblyRef = metadataReader.GetAssemblyReference(assemblyRefHandle);
            var refName = metadataReader.GetString(assemblyRef.Name);
            var refVersion = assemblyRef.Version;
            var refCulture = metadataReader.GetString(assemblyRef.Culture);
            var refPublicKeyToken = metadataReader.GetBlobBytes(assemblyRef.PublicKeyOrToken);

            graph.References.Add(new AssemblyReferenceDto
            {
                Name = refName,
                Version = refVersion.ToString(),
                Culture = string.IsNullOrEmpty(refCulture) ? "neutral" : refCulture,
                PublicKeyToken = GetPublicKeyToken(refPublicKeyToken)
            });
        }

        return Task.FromResult(graph);
    }

    private static string GetPublicKeyToken(byte[] publicKeyToken)
    {
        if (publicKeyToken == null || publicKeyToken.Length == 0)
            return "null";

        return BitConverter.ToString(publicKeyToken).Replace("-", "").ToLower();
    }

    private static string GetPublicKeyTokenFromBlob(byte[] publicKey)
    {
        if (publicKey == null || publicKey.Length == 0)
            return "null";

        // If it's already a token (8 bytes)
        if (publicKey.Length == 8)
            return GetPublicKeyToken(publicKey);

        // If it's a full public key, calculate the token
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(publicKey);
        var token = hash.Skip(hash.Length - 8).Reverse().ToArray();
        return GetPublicKeyToken(token);
    }
}
