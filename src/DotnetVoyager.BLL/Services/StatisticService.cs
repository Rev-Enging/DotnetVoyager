using DotnetVoyager.BLL.Dtos;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using Mono.Cecil;

namespace DotnetVoyager.BLL.Services;

public interface IStatisticsService
{
    Task<AssemblyStatisticsDto> GetAssemblyStatisticsAsync(string assemblyPath);
}

public class StatisticsService : IStatisticsService
{
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


        return Task.FromResult(statistics);
    }

    private static int CalculateDecompiledLinesOfCode(string assemblyPath)
    {
        var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings());
        var fullCode = decompiler.DecompileWholeModuleAsString();
        return fullCode.Count(c => c == '\n') + 1;
    }
}

/*public class StatisticsService : IStatisticsService
{
    public Task<AssemblyStatisticsDto> GetAssemblyStatisticsAsync(string assemblyPath)
    {
        var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings());
        var allTypes = decompiler.TypeSystem.MainModule.TypeDefinitions;

        var statistics = new AssemblyStatisticsDto
        {
            NamespaceCount = allTypes.Select(t => t.Namespace).Distinct().Count(),
            ClassCount = allTypes.Count(t => t.Kind == TypeKind.Class),
            InterfaceCount = allTypes.Count(t => t.Kind == TypeKind.Interface),
            StructCount = allTypes.Count(t => t.Kind == TypeKind.Struct),
            MethodCount = allTypes.SelectMany(t => t.Methods).Count(),
            IlInstructionCount = CalculateIlInstructions(allTypes),
            DecompiledLinesOfCode = CalculateDecompiledLinesOfCode(decompiler)
        };

        return Task.FromResult(statistics);
    }

    private int CalculateIlInstructions(IEnumerable<ITypeDefinition> types)
    {
        // English comments as requested in user profile
        // Sums up the IL instructions from all method bodies
        return types.SelectMany(t => t.Methods)
                    .Where(m => m.HasBody)
                    .Select(m => m.Get)
                    .Where(mb => mb != null)
                    .Sum(mb => mb.Instructions.Count);
    }

    private int CalculateDecompiledLinesOfCode(CSharpDecompiler decompiler)
    {
        // English comments as requested in user profile
        // Decompiles the entire assembly to a single string and counts newlines.
        // This is a performance-intensive operation.
        var fullCode = decompiler.DecompileWholeModuleAsString();
        return fullCode.Count(c => c == '\n') + 1;
    }
}*/
