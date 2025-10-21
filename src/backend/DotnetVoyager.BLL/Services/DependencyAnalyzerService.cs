using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services;

public class DependencyGraphDto
{
    public string RootAssembly { get; set; }
    public string Version { get; set; }
    public string Culture { get; set; }
    public string PublicKeyToken { get; set; }
    public List<DependencyInfoDto> Dependencies { get; set; }
}

public class DependencyInfoDto
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Culture { get; set; }
    public string PublicKeyToken { get; set; }
}

public interface IDependencyAnalyzerService
{
    DependencyGraphDto AnalyzeAssemblyDependencies(string assemblyPath);
}

public class DependencyAnalyzerService : IDependencyAnalyzerService
{
    public DependencyGraphDto AnalyzeAssemblyDependencies(string assemblyPath)
    {
        using var fileStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var peReader = new PEReader(fileStream);

        var metadataReader = peReader.GetMetadataReader();

        var assemblyDef = metadataReader.GetAssemblyDefinition();
        var assemblyName = metadataReader.GetString(assemblyDef.Name);
        var version = assemblyDef.Version;
        var culture = metadataReader.GetString(assemblyDef.Culture);

        var graph = new DependencyGraphDto
        {
            RootAssembly = assemblyName,
            Version = version.ToString(),
            Culture = string.IsNullOrEmpty(culture) ? "neutral" : culture,
            PublicKeyToken = GetPublicKeyTokenFromBlob(metadataReader.GetBlobBytes(assemblyDef.PublicKey)),
            Dependencies = new List<DependencyInfoDto>()
        };

        // Отримуємо всі залежності
        foreach (var assemblyRefHandle in metadataReader.AssemblyReferences)
        {
            var assemblyRef = metadataReader.GetAssemblyReference(assemblyRefHandle);
            var refName = metadataReader.GetString(assemblyRef.Name);
            var refVersion = assemblyRef.Version;
            var refCulture = metadataReader.GetString(assemblyRef.Culture);
            var refPublicKeyToken = metadataReader.GetBlobBytes(assemblyRef.PublicKeyOrToken);

            graph.Dependencies.Add(new DependencyInfoDto
            {
                Name = refName,
                Version = refVersion.ToString(),
                Culture = string.IsNullOrEmpty(refCulture) ? "neutral" : refCulture,
                PublicKeyToken = GetPublicKeyToken(refPublicKeyToken)
            });
        }

        return graph;
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

        // Якщо це вже токен (8 байтів)
        if (publicKey.Length == 8)
            return GetPublicKeyToken(publicKey);

        // Якщо це повний публічний ключ, обчислюємо токен
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = sha1.ComputeHash(publicKey);
        var token = hash.Skip(hash.Length - 8).Reverse().ToArray();
        return GetPublicKeyToken(token);
    }
}
