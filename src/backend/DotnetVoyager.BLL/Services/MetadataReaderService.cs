using DotnetVoyager.BLL.Dtos;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services;

public interface IMetadataReaderService
{
    Task<AssemblyMetadataDto> GetAssemblyMetadataAsync(string assemblyPath, CancellationToken token = default);
}

public class MetadataReaderService : IMetadataReaderService
{
    public async Task<AssemblyMetadataDto> GetAssemblyMetadataAsync(string assemblyPath, CancellationToken token = default)
    {
        // English comments as requested in user profile
        // Using a byte array is more robust than a FileStream for PEReader
        var fileBytes = await File.ReadAllBytesAsync(assemblyPath);
        using var peReader = new PEReader(new MemoryStream(fileBytes));
        var metadataReader = peReader.GetMetadataReader();
        var assemblyDef = metadataReader.GetAssemblyDefinition();

        // Get dependencies by reading assembly references
        var dependencies = metadataReader.AssemblyReferences
            .Select(handle => metadataReader.GetString(metadataReader.GetAssemblyReference(handle).Name))
            .ToList();

        var metadata = new AssemblyMetadataDto
        {
            AssemblyName = metadataReader.GetString(assemblyDef.Name),
            Version = assemblyDef.Version.ToString(),
            TargetFramework = GetTargetFramework(metadataReader),
            Architecture = GetArchitecture(peReader.PEHeaders.CoffHeader.Machine),
            Dependencies = dependencies
        };

        return metadata;
    }

    private static string GetTargetFramework(MetadataReader reader)
    {
        var attributeHandle = reader.CustomAttributes
            .Select(reader.GetCustomAttribute)
            .FirstOrDefault(attr => {
                if (attr.Constructor.Kind != HandleKind.MemberReference) return false;
                var memberRef = reader.GetMemberReference((MemberReferenceHandle)attr.Constructor);
                var typeRefHandle = memberRef.Parent;
                if (typeRefHandle.Kind != HandleKind.TypeReference) return false;
                var typeRef = reader.GetTypeReference((TypeReferenceHandle)typeRefHandle);
                return reader.GetString(typeRef.Name) == "TargetFrameworkAttribute";
            });

        if (attributeHandle.Equals(default)) return "Unknown";

        var blobReader = reader.GetBlobReader(attributeHandle.Value);
        // Skip the two-byte blob prolog
        blobReader.ReadByte();
        blobReader.ReadByte();
        return blobReader.ReadSerializedString() ?? "Unknown";
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