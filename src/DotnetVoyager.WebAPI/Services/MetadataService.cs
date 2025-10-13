using DotnetVoyager.WebAPI.Dtos;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;

namespace DotnetVoyager.WebAPI.Services;

public interface IMetadataService
{
    AssemblyDetailsDto GetAssemblyDetails(string assemblyPath);
}

public class MetadataService : IMetadataService
{
    public AssemblyDetailsDto GetAssemblyDetails(string assemblyPath)
    {
        // Part 1: Use Mono.Cecil for statistics and dependencies
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        var mainModule = assemblyDefinition.MainModule;

        var statistics = new AssemblyStatisticsDto
        {
            NamespaceCount = mainModule.Types.Select(t => t.Namespace).Distinct().Count(),
            ClassCount = mainModule.Types.Count(t => t.IsClass && !t.IsInterface),
            InterfaceCount = mainModule.Types.Count(t => t.IsInterface),
            StructCount = mainModule.Types.Count(t => t.IsValueType && !t.IsPrimitive && !t.IsEnum),
            MethodCount = mainModule.Types.SelectMany(t => t.Methods).Count(),
            LinesOfCode = 0 // Placeholder for now
        };

        var dependencies = mainModule.AssemblyReferences
                                     .Select(ar => ar.Name)
                                     .ToList();

        // Part 2: Use PEReader for TargetFramework and Architecture
        using var fs = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var peReader = new PEReader(fs);
        var metadataReader = peReader.GetMetadataReader();

        var assemblyName = metadataReader.GetString(metadataReader.GetAssemblyDefinition().Name);
        var version = metadataReader.GetAssemblyDefinition().Version.ToString();

        var details = new AssemblyDetailsDto
        {
            AssemblyName = assemblyName,
            Version = version,
            TargetFramework = GetTargetFramework(metadataReader),
            Architecture = GetArchitecture(peReader.PEHeaders.CoffHeader.Machine),
            Dependencies = dependencies,
            Statistics = statistics
        };

        return details;
    }

    private string GetTargetFramework(MetadataReader reader)
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

    private string GetArchitecture(Machine machine) => machine switch
    {
        Machine.Amd64 => "x64",
        Machine.I386 => "x86",
        Machine.Arm => "ARM",
        Machine.Arm64 => "ARM64",
        _ => "Any CPU"
    };
}