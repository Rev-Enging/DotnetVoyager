using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace DotnetVoyager.BLL.Factories;

/// <summary>
/// A factory for creating CSharpDecompiler instances
/// with the correct, shared configuration.
/// </summary>
public interface IDecompilerFactory
{
    /// <summary>
    /// Creates a new CSharpDecompiler instance with a "lenient"
    /// assembly resolver that does not throw errors on missing dependencies.
    /// </summary>
    /// <param name="assemblyPath">The path to the main assembly file.</param>
    /// <returns>A configured CSharpDecompiler instance.</returns>
    CSharpDecompiler Create(string assemblyPath);
}

public class DecompilerFactory : IDecompilerFactory
{
    public CSharpDecompiler Create(string assemblyPath)
    {
        var resolver = new UniversalAssemblyResolver(
            assemblyPath,
            throwOnError: false,
            targetFramework: null
        );

        var settings = new DecompilerSettings();

        return new CSharpDecompiler(
            assemblyPath,
            resolver,
            settings
        );
    }
}