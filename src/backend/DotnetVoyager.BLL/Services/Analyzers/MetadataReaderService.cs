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
        // Optimization 1: Use FileStream with FileShare.Read. 
        // This prevents loading the whole file into RAM (LOH protection).
        // We use FileShare.Read/Delete to ensure we don't lock the file against the cleanup service.
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

        using var peReader = new PEReader(stream);

        if (!peReader.HasMetadata)
        {
            throw new InvalidOperationException("The provided file does not contain CLI metadata.");
        }

        var metadataReader = peReader.GetMetadataReader();
        var assemblyDef = metadataReader.GetAssemblyDefinition();

        // Optimization 3: Pre-allocate List capacity to avoid resizing
        var referenceHandles = metadataReader.AssemblyReferences;
        var dependencies = new List<string>(referenceHandles.Count);

        foreach (var handle in referenceHandles)
        {
            var reference = metadataReader.GetAssemblyReference(handle);
            dependencies.Add(metadataReader.GetString(reference.Name));
        }

        var metadata = new AssemblyMetadataDto
        {
            AssemblyName = metadataReader.GetString(assemblyDef.Name),
            Version = assemblyDef.Version.ToString(),
            TargetFramework = GetTargetFramework(metadataReader),
            Architecture = GetArchitecture(peReader.PEHeaders.CoffHeader.Machine),
            Dependencies = dependencies
        };

        return Task.FromResult(metadata);
    }

    private static string GetTargetFramework(MetadataReader reader)
    {
        // Optimization 2: Replace LINQ with a foreach loop to reduce memory allocations
        foreach (var handle in reader.CustomAttributes)
        {
            var attr = reader.GetCustomAttribute(handle);

            // Check if the constructor is a MemberReference (typical for external attributes like TargetFramework)
            if (attr.Constructor.Kind != HandleKind.MemberReference) continue;

            var memberRef = reader.GetMemberReference((MemberReferenceHandle)attr.Constructor);
            var typeRefHandle = memberRef.Parent;

            if (typeRefHandle.Kind != HandleKind.TypeReference) continue;

            var typeRef = reader.GetTypeReference((TypeReferenceHandle)typeRefHandle);

            // Quick check on the name before reading the blob
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
            var blobReader = reader.GetBlobReader(valueHandle);

            // Prolog check (must be 0x0001)
            if (blobReader.ReadUInt16() == 0x0001)
            {
                // The first argument of TargetFrameworkAttribute is the framework name string
                return blobReader.ReadSerializedString() ?? "Unknown";
            }
        }
        catch
        {
            // Fallback in case of malformed blob
        }
        return "Unknown";
    }

    private static string GetArchitecture(Machine machine) => machine switch
    {
        Machine.Amd64 => "x64",
        Machine.I386 => "x86",
        Machine.Arm => "ARM",
        Machine.Arm64 => "ARM64",
        _ => "Any CPU" // Or specific mapping for Bit32/Itanium if needed
    };
}