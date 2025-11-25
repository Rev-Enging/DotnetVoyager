using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;

namespace DotnetVoyager.BLL.Services;

/// <summary>
/// Provides functionality to decompile an entire assembly into a project structure.
/// </summary>
public interface IFullDecompilationService
{
    /// <summary>
    /// Decompiles an assembly and saves the resulting source code
    /// (including .cs files and a .csproj) to a specified directory.
    /// </summary>
    /// <param name="assemblyPath">The file path to the .dll or .exe to decompile.</param>
    /// <param name="outputDirectoryPath">The directory where the decompiled project will be saved.</param>
    /// <param name="token">A cancellation token.</param>
    Task DecompileProjectAsync(string assemblyPath, string outputDirectoryPath, CancellationToken token = default);
}

public class FullDecompilationService : IFullDecompilationService
{
    public Task DecompileProjectAsync(string assemblyPath, string outputDirectoryPath, CancellationToken token = default)
    {
        return Task.Run(() =>
        {
            Directory.CreateDirectory(outputDirectoryPath);

            // Optimization: Shared file access + Delete permission
            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var peFile = new PEFile(assemblyPath, stream);

            var targetFramework = peFile.DetectTargetFrameworkId();

            var resolver = new UniversalAssemblyResolver(
                assemblyPath,
                throwOnError: false,
                targetFramework: targetFramework
            );

            var searchDir = Path.GetDirectoryName(assemblyPath);
            if (!string.IsNullOrEmpty(searchDir))
            {
                resolver.AddSearchDirectory(searchDir);
            }

            // ВИПРАВЛЕННЯ ТУТ:
            // 1. Використовуємо простий конструктор, що приймає тільки resolver
            var decompiler = new WholeProjectDecompiler(resolver);

            // 2. Налаштовуємо Settings напряму через властивість об'єкта, 
            // замість передачі окремого об'єкта settings у конструктор.
            decompiler.Settings.ThrowOnAssemblyResolveErrors = false;
            decompiler.Settings.UseSdkStyleProjectFormat = true;
            decompiler.Settings.UseNestedDirectoriesForNamespaces = true;

            // LanguageVersion видалено, бо бібліотека тепер визначає це сама.

            // Запускаємо декомпіляцію
            decompiler.DecompileProject(peFile, outputDirectoryPath, token);

        }, token);
    }
}

/*public class FullDecompilationService : IFullDecompilationService
{
    public async Task DecompileProjectAsync(string assemblyPath, string outputDirectoryPath, CancellationToken token = default)
    {
        await Task.Run(() =>
        {
            // Створюємо директорію, якщо не існує
            Directory.CreateDirectory(outputDirectoryPath);

            // Налаштовуємо resolver
            var resolver = new UniversalAssemblyResolver(
                assemblyPath,
                throwOnError: false,
                targetFramework: null
            );
            
            var searchDir = Path.GetDirectoryName(assemblyPath);
            if (!string.IsNullOrEmpty(searchDir))
            {
                resolver.AddSearchDirectory(searchDir);
            }

            // Налаштовуємо DecompilerSettings
            var settings = new DecompilerSettings
            {
                ThrowOnAssemblyResolveErrors = false,
                UseSdkStyleProjectFormat = true,
                UseNestedDirectoriesForNamespaces = true
            };

            // Створюємо project writer

            // Створюємо WholeProjectDecompiler з правильними параметрами
            var decompiler = new WholeProjectDecompiler(
                settings,
                resolver,
                null,
                null, // debugInfoProvider
                null  // assemblyReferenceClassifier
            );
            
            // Запускаємо декомпіляцію
            using var module = new PEFile(assemblyPath);
            decompiler.DecompileProject(module, outputDirectoryPath, token);
            
        }, token);
    }
}*/