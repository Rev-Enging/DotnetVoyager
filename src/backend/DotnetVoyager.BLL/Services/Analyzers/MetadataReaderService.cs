using DotnetVoyager.BLL.Dtos.AnalysisResults;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IMetadataReaderService
{
    Task<AssemblyMetadataDto> GetAssemblyMetadataAsync(string assemblyPath, CancellationToken token = default);
}

public class MetadataReaderService : IMetadataReaderService
{
    public Task<AssemblyMetadataDto> GetAssemblyMetadataAsync(string assemblyPath, CancellationToken token = default)
    {
        // Open file with shared access to avoid locking it.
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var peReader = new PEReader(stream);

        if (!peReader.HasMetadata)
        {
            throw new InvalidOperationException("File contains no CLI metadata.");
        }

        var reader = peReader.GetMetadataReader();
        var assemblyDef = reader.GetAssemblyDefinition();

        // Collect external assembly references.
        var dependencies = new List<string>();
        foreach (var handle in reader.AssemblyReferences)
        {
            var reference = reader.GetAssemblyReference(handle);
            dependencies.Add(reader.GetString(reference.Name));
        }

        return Task.FromResult(new AssemblyMetadataDto
        {
            AssemblyName = reader.GetString(assemblyDef.Name),
            Version = assemblyDef.Version.ToString(),
            TargetFramework = GetTargetFramework(reader),
            Architecture = GetArchitecture(peReader.PEHeaders.CoffHeader.Machine),
            Dependencies = dependencies
        });
    }

    private static string GetTargetFramework(MetadataReader reader)
    {
        foreach (var handle in reader.CustomAttributes)
        {
            var attr = reader.GetCustomAttribute(handle);

            // Filter for attributes defined in external assemblies.
            if (attr.Constructor.Kind != HandleKind.MemberReference) continue;

            var memberRef = reader.GetMemberReference((MemberReferenceHandle)attr.Constructor);
            if (memberRef.Parent.Kind != HandleKind.TypeReference) continue;

            var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);

            // Check if the attribute is TargetFrameworkAttribute.
            if (reader.GetString(typeRef.Name) == "TargetFrameworkAttribute")
            {
                return DecodeTargetFrameworkBlob(reader, attr.Value);
            }
        }
        return "Unknown";
    }

    private static string DecodeTargetFrameworkBlob(MetadataReader reader, BlobHandle valueHandle)
    {
        try
        {
            var blob = reader.GetBlobReader(valueHandle);

            // 0x0001 is the standard prolog for custom attribute blobs.
            if (blob.ReadUInt16() == 0x0001)
            {
                return blob.ReadSerializedString() ?? "Unknown";
            }
        }
        catch { /* Ignore parsing errors */ }

        return "Unknown";
    }

    private static string GetArchitecture(Machine machine) => machine switch
    {
        Machine.Amd64 => "x64",
        Machine.I386 => "x86",
        Machine.Arm => "ARM",
        Machine.Arm64 => "ARM64",
        _ => "Any CPU"
    };
}