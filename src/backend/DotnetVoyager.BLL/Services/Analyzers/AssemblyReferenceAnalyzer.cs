using DotnetVoyager.BLL.Dtos.AnalysisResults;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IAssemblyReferenceAnalyzer
{
    Task<AssemblyDependenciesDto> AnalyzeReferences(string assemblyPath);
}

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

            // FIX: Since fields are required, we cannot return an empty object.
            // If metadata is missing, the file is invalid, so we throw.
            // The AnalysisWorker will catch this and mark the step as "Failed".
            if (!peReader.HasMetadata)
            {
                throw new BadImageFormatException("The provided file does not contain CLI metadata.");
            }

            var metadataReader = peReader.GetMetadataReader();
            var assemblyDef = metadataReader.GetAssemblyDefinition();

            var refsCount = metadataReader.AssemblyReferences.Count;
            var references = new List<AssemblyReferenceDto>(refsCount);

            var pkBlob = metadataReader.GetBlobBytes(assemblyDef.PublicKey);

            // Now we construct the object with all REQUIRED fields
            var graph = new AssemblyDependenciesDto
            {
                AssemblyName = metadataReader.GetString(assemblyDef.Name),
                Version = assemblyDef.Version.ToString(),
                Culture = GetCultureString(metadataReader, assemblyDef.Culture),
                PublicKeyToken = GetPublicKeyTokenFromBlob(pkBlob),
                References = references
            };

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

    private static string GetCultureString(MetadataReader reader, StringHandle handle)
    {
        if (handle.IsNil) return "neutral";
        var culture = reader.GetString(handle);
        return string.IsNullOrEmpty(culture) ? "neutral" : culture;
    }

    /// <summary>
    /// Converts a Public Key (full) or Public Key Token (short) into a hex string.
    /// Uses Span and stackalloc to avoid memory allocations.
    /// </summary>
    private static string GetPublicKeyTokenFromBlob(byte[] publicKeyData)
    {
        if (publicKeyData == null || publicKeyData.Length == 0)
            return "null";

        // Case 1: It is already a Token (8 bytes)
        // Usually found in Assembly References (PublicKeyOrToken)
        if (publicKeyData.Length == 8)
        {
            return ToHexString(publicKeyData);
        }

        // Case 2: It is a Full Public Key
        // Usually found in Assembly Definition. We need to hash it to get the Token.
        // Algorithm: SHA1 -> Take last 8 bytes -> Reverse -> ToHex

        // Optimization 4: Use static SHA1.HashData (available in .NET 6+) 
        // to avoid allocating SHA1 object instance.
        Span<byte> hash = stackalloc byte[20]; // SHA1 is always 20 bytes
        SHA1.HashData(publicKeyData, hash);

        // Token is the last 8 bytes of the SHA1 hash, reversed
        Span<byte> tokenSpan = hash.Slice(hash.Length - 8);
        tokenSpan.Reverse();

        return ToHexString(tokenSpan);
    }

    // Optimization 5: Fast Hex conversion without BitConverter allocations
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

    private static char ToHexChar(int n)
    {
        return (char)(n < 10 ? n + '0' : n - 10 + 'a');
    }
}

/*public class AssemblyReferenceAnalyzer : IAssemblyReferenceAnalyzer
{
    public Task<AssemblyDependenciesDto> AnalyzeReferences(string assemblyPath)
    {
        using var fileStream = new FileStream(
            assemblyPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

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
}*/
