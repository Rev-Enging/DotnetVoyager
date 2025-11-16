using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Factories;
using Mono.Cecil;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IStatisticsAnalyzer
{
    Task<AssemblyStatisticsDto> GetAssemblyStatisticsAsync(string assemblyPath);
}

public class StatisticsAnalyzer : IStatisticsAnalyzer
{
    private readonly IDecompilerFactory _decompilerFactory;

    public StatisticsAnalyzer(IDecompilerFactory decompilerFactory)
    {
        _decompilerFactory = decompilerFactory;
    }

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

    private int CalculateDecompiledLinesOfCode(string assemblyPath)
    {
        var decompiler = _decompilerFactory.Create(assemblyPath);
        var fullCode = decompiler.DecompileWholeModuleAsString();
        return fullCode.Count(c => c == '\n') + 1;
    }
}
