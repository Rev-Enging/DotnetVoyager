using DotnetVoyager.BLL.Dtos;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata; // <--- ДОДАЙТЕ ЦЕЙ USING
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

            // Цей виклик тепер безпечний
            DecompiledLinesOfCode = CalculateDecompiledLinesOfCode(assemblyPath)
        };

        return Task.FromResult(statistics);
    }

    private static int CalculateDecompiledLinesOfCode(string assemblyPath)
    {
        // 1. Створюємо "поблажливий" AssemblyResolver,
        //    який не буде падати, якщо не знайде залежність.
        var resolver = new UniversalAssemblyResolver(
            assemblyPath,       // Допомагає йому знайти залежності, якщо вони лежать поруч
            throwOnError: false, // <-- ОСЬ ГОЛОВНЕ ВИПРАВЛЕННЯ
            targetFramework: null // Автоматичне визначення
        );

        var settings = new DecompilerSettings();

        // 2. Створюємо декомпільовтор, передаючи йому наш новий resolver
        var decompiler = new CSharpDecompiler(
            assemblyPath,
            resolver, // <-- Передаємо наш resolver
            settings
        );

        var fullCode = decompiler.DecompileWholeModuleAsString();
        return fullCode.Count(c => c == '\n') + 1;
    }
}

/*using DotnetVoyager.BLL.Dtos;
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
*/