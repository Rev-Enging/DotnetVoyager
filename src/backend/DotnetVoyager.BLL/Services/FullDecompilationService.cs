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

            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

            using var peFile = new PEFile(assemblyPath, stream);

            var targetFramework = peFile.DetectTargetFrameworkId();

            var resolver = new UniversalAssemblyResolver(
                assemblyPath,
                throwOnError: false, // Do not crash if a dependency is missing.
                targetFramework: targetFramework
            );

            var searchDir = Path.GetDirectoryName(assemblyPath);
            if (!string.IsNullOrEmpty(searchDir))
            {
                resolver.AddSearchDirectory(searchDir);
            }

            // Create the decompiler engine specifically for generating whole projects (.csproj).
            var decompiler = new WholeProjectDecompiler(resolver);

            // Ignore errors when references cannot be found (prevents partial failure).
            decompiler.Settings.ThrowOnAssemblyResolveErrors = false;
            // Use the modern .csproj format (SDK-style) which is cleaner and standard for .NET Core/5+.
            decompiler.Settings.UseSdkStyleProjectFormat = true;
            // Create subfolders for namespaces (e.g., namespace 'App.Models' -> folder 'App/Models').
            decompiler.Settings.UseNestedDirectoriesForNamespaces = true;

            decompiler.DecompileProject(peFile, outputDirectoryPath, token);

        }, token);
    }
}