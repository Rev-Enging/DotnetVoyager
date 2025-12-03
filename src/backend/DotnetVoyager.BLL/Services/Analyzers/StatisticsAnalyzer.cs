using DotnetVoyager.BLL.Dtos.AnalysisResults;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IStatisticsAnalyzer
{
    Task<AssemblyStatisticsDto> GetAssemblyStatisticsAsync(string assemblyPath);
}

public class StatisticsAnalyzer : IStatisticsAnalyzer
{
    public Task<AssemblyStatisticsDto> GetAssemblyStatisticsAsync(string assemblyPath)
    {
        // Offload heavy metadata parsing to a background thread.
        return Task.Run(() => AnalyzeAssembly(assemblyPath));
    }

    private static AssemblyStatisticsDto AnalyzeAssembly(string assemblyPath)
    {
        using var fileStream = new FileStream(
            assemblyPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete);

        using var peReader = new PEReader(fileStream);

        ValidateMetadata(peReader);

        // MetadataReader provides low-level access to the logical structure (tables) of the assembly.
        var metadataReader = peReader.GetMetadataReader();
        var collector = new StatisticsCollector();

        CollectTypeStatistics(metadataReader, collector);

        return collector.ToDto();
    }

    private static void ValidateMetadata(PEReader peReader)
    {
        if (!peReader.HasMetadata)
        {
            throw new InvalidOperationException("The provided file does not contain CLI metadata.");
        }
    }

    private static void CollectTypeStatistics(MetadataReader metadataReader, StatisticsCollector collector)
    {
        // Iterate through all type definitions (classes, structs, interfaces, enums) in the assembly.
        foreach (var typeDefHandle in metadataReader.TypeDefinitions)
        {
            var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);

            if (ShouldSkipType(typeDef))
                continue;

            ProcessType(metadataReader, typeDef, collector);
        }
    }

    private static bool ShouldSkipType(TypeDefinition typeDef)
    {
        // Skip compiler-generated types (e.g., closures for lambdas) marked with SpecialName.
        return (typeDef.Attributes & TypeAttributes.SpecialName) != 0;
    }

    private static void ProcessType(MetadataReader metadataReader, TypeDefinition typeDef, StatisticsCollector collector)
    {
        CollectNamespace(metadataReader, typeDef, collector);
        ClassifyAndCountType(metadataReader, typeDef, collector);
        CountMethods(typeDef, collector);
    }

    private static void CollectNamespace(MetadataReader metadataReader, TypeDefinition typeDef, StatisticsCollector collector)
    {
        if (typeDef.Namespace.IsNil) return;

        // Retrieve the string value of the namespace from the metadata heap.
        var ns = metadataReader.GetString(typeDef.Namespace);

        if (!string.IsNullOrEmpty(ns))
        {
            collector.AddNamespace(ns);
        }
    }

    private static void ClassifyAndCountType(MetadataReader metadataReader, TypeDefinition typeDef, StatisticsCollector collector)
    {
        var attrs = typeDef.Attributes;

        if (IsInterface(attrs))
        {
            collector.IncrementInterfaces();
            return;
        }

        // If it's not a class (e.g., delegate), skip it.
        if (!IsClass(attrs)) return;

        // In Metadata, structs are defined as sealed classes that inherit from System.ValueType.
        if (IsStruct(metadataReader, typeDef, attrs))
        {
            collector.IncrementStructs();
        }
        else
        {
            collector.IncrementClasses();
        }
    }

    private static bool IsInterface(TypeAttributes attrs)
    {
        return (attrs & TypeAttributes.Interface) != 0;
    }

    private static bool IsClass(TypeAttributes attrs)
    {
        // Ensure it uses class semantics (not an interface).
        return (attrs & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
    }

    private static bool IsStruct(MetadataReader metadataReader, TypeDefinition typeDef, TypeAttributes attrs)
    {
        // Structs must be sealed.
        var isSealed = (attrs & TypeAttributes.Sealed) != 0;
        return isSealed && IsValueType(metadataReader, typeDef);
    }

    private static void CountMethods(TypeDefinition typeDef, StatisticsCollector collector)
    {
        // Iterate over method handles belonging to this type.
        foreach (var _ in typeDef.GetMethods())
        {
            collector.IncrementMethods();
        }
    }

    private static bool IsValueType(MetadataReader reader, TypeDefinition typeDef)
    {
        // Check if the base type exists and is a reference to another type (System.ValueType).
        if (typeDef.BaseType.IsNil || typeDef.BaseType.Kind != HandleKind.TypeReference)
            return false;

        var typeRef = reader.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
        return IsSystemValueTypeOrEnum(reader, typeRef);
    }

    private static bool IsSystemValueTypeOrEnum(MetadataReader reader, TypeReference typeRef)
    {
        try
        {
            // Resolve names from string heap to identify base type.
            var typeName = reader.GetString(typeRef.Name);
            var typeNamespace = reader.GetString(typeRef.Namespace);

            return typeNamespace == "System" && (typeName == "ValueType" || typeName == "Enum");
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class StatisticsCollector
{
    private readonly HashSet<string> _uniqueNamespaces = new();
    private int _classCount;
    private int _interfaceCount;
    private int _structCount;
    private int _methodCount;

    public void AddNamespace(string ns) => _uniqueNamespaces.Add(ns);
    public void IncrementClasses() => _classCount++;
    public void IncrementInterfaces() => _interfaceCount++;
    public void IncrementStructs() => _structCount++;
    public void IncrementMethods() => _methodCount++;

    public AssemblyStatisticsDto ToDto() => new()
    {
        NamespaceCount = _uniqueNamespaces.Count,
        ClassCount = _classCount,
        InterfaceCount = _interfaceCount,
        StructCount = _structCount,
        MethodCount = _methodCount
    };
}