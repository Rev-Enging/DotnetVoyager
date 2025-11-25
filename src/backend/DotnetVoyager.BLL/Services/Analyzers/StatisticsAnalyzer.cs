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
        return Task.Run(() => AnalyzeAssembly(assemblyPath));
    }

    // Refactored: Main analysis logic extracted to reduce complexity
    private static AssemblyStatisticsDto AnalyzeAssembly(string assemblyPath)
    {
        using var fileStream = OpenAssemblyFile(assemblyPath);
        using var peReader = new PEReader(fileStream);

        ValidateMetadata(peReader);

        var metadataReader = peReader.GetMetadataReader();
        var collector = new StatisticsCollector();

        CollectTypeStatistics(metadataReader, collector);

        return collector.ToDto();
    }

    // Refactored: File opening logic separated
    private static FileStream OpenAssemblyFile(string assemblyPath)
    {
        return new FileStream(
            assemblyPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete);
    }

    // Refactored: Validation logic separated
    private static void ValidateMetadata(PEReader peReader)
    {
        if (!peReader.HasMetadata)
        {
            throw new InvalidOperationException(
                "The provided file does not contain CLI metadata.");
        }
    }

    // Refactored: Main collection loop simplified
    private static void CollectTypeStatistics(
        MetadataReader metadataReader,
        StatisticsCollector collector)
    {
        foreach (var typeDefHandle in metadataReader.TypeDefinitions)
        {
            var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);

            if (ShouldSkipType(typeDef))
                continue;

            ProcessType(metadataReader, typeDef, collector);
        }
    }

    // Refactored: Type filtering logic separated
    private static bool ShouldSkipType(TypeDefinition typeDef)
    {
        return (typeDef.Attributes & TypeAttributes.SpecialName) != 0;
    }

    // Refactored: Type processing logic separated
    private static void ProcessType(
        MetadataReader metadataReader,
        TypeDefinition typeDef,
        StatisticsCollector collector)
    {
        CollectNamespace(metadataReader, typeDef, collector);
        ClassifyAndCountType(metadataReader, typeDef, collector);
        CountMethods(typeDef, collector);
    }

    // Refactored: Namespace collection separated
    private static void CollectNamespace(
        MetadataReader metadataReader,
        TypeDefinition typeDef,
        StatisticsCollector collector)
    {
        if (typeDef.Namespace.IsNil)
            return;

        var ns = metadataReader.GetString(typeDef.Namespace);

        if (!string.IsNullOrEmpty(ns))
        {
            collector.AddNamespace(ns);
        }
    }

    // Refactored: Type classification logic separated and simplified
    private static void ClassifyAndCountType(
        MetadataReader metadataReader,
        TypeDefinition typeDef,
        StatisticsCollector collector)
    {
        var attrs = typeDef.Attributes;

        if (IsInterface(attrs))
        {
            collector.IncrementInterfaces();
            return;
        }

        if (!IsClass(attrs))
            return;

        if (IsStruct(metadataReader, typeDef, attrs))
        {
            collector.IncrementStructs();
        }
        else
        {
            collector.IncrementClasses();
        }
    }

    // Refactored: Simple predicate methods
    private static bool IsInterface(TypeAttributes attrs)
    {
        return (attrs & TypeAttributes.Interface) != 0;
    }

    private static bool IsClass(TypeAttributes attrs)
    {
        return (attrs & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class;
    }

    private static bool IsStruct(
        MetadataReader metadataReader,
        TypeDefinition typeDef,
        TypeAttributes attrs)
    {
        var isSealed = (attrs & TypeAttributes.Sealed) != 0;
        return isSealed && IsValueType(metadataReader, typeDef);
    }

    // Refactored: Method counting separated
    private static void CountMethods(TypeDefinition typeDef, StatisticsCollector collector)
    {
        foreach (var _ in typeDef.GetMethods())
        {
            collector.IncrementMethods();
        }
    }

    // Refactored: ValueType check simplified
    private static bool IsValueType(MetadataReader reader, TypeDefinition typeDef)
    {
        if (typeDef.BaseType.IsNil)
            return false;

        if (typeDef.BaseType.Kind != HandleKind.TypeReference)
            return false;

        var typeRef = reader.GetTypeReference((TypeReferenceHandle)typeDef.BaseType);
        return IsSystemValueTypeOrEnum(reader, typeRef);
    }

    // Refactored: Final check separated
    private static bool IsSystemValueTypeOrEnum(MetadataReader reader, TypeReference typeRef)
    {
        try
        {
            var typeName = reader.GetString(typeRef.Name);
            var typeNamespace = reader.GetString(typeRef.Namespace);

            return typeNamespace == "System" &&
                   (typeName == "ValueType" || typeName == "Enum");
        }
        catch
        {
            return false;
        }
    }
}

// Refactored: Statistics collection encapsulated in a separate class
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

/*public class StatisticsAnalyzer(IDecompilerFactory decompilerFactory) : IStatisticsAnalyzer
{
    private readonly IDecompilerFactory _decompilerFactory = decompilerFactory;

    public Task<AssemblyStatisticsDto> GetAssemblyStatisticsAsync(string assemblyPath)
    {
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        var mainModule = assemblyDefinition.MainModule;

        var statistics = new AssemblyStatisticsDto
        {
            NamespaceCount = mainModule.Types.Select(t => t.Namespace).Distinct().Count(),
            ClassCount = mainModule.Types.Count(t => t.IsClass && !t.IsInterface),
            InterfaceCount = mainModule.Types.Count(t => t.IsInterface),
            StructCount = mainModule.Types.Count(t => t.IsValueType && !t.IsPrimitive && !t.IsEnum),
            MethodCount = mainModule.Types.SelectMany(t => t.Methods).Count(),

            IlInstructionCount = mainModule.Types
                .SelectMany(t => t.Methods)
                .Where(m => m.HasBody)
                .Sum(m => m.Body.Instructions.Count),

            DecompiledLinesOfCode = CalculateDecompiledLinesOfCode(assemblyPath)
        };
        assemblyDefinition.Dispose();
        return Task.FromResult(statistics);
    }

    private int CalculateDecompiledLinesOfCode(string assemblyPath)
    {
        var decompiler = _decompilerFactory.Create(assemblyPath);
        var fullCode = decompiler.DecompileWholeModuleAsString();
        return fullCode.Count(c => c == '\n') + 1;
    }
}*/
