using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Factories;
using Mono.Cecil;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface ICodeDecompilationService
{
    Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token);
}

public class CodeDecompilationService : ICodeDecompilationService
{
    private readonly IDecompilerFactory _decompilerFactory;

    public CodeDecompilationService(IDecompilerFactory decompilerFactory)
    {
        _decompilerFactory = decompilerFactory;
    }

    public Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token)
    {
        var handle = MetadataTokens.EntityHandle(token);

        var decompiler = _decompilerFactory.Create(assemblyPath);

        var csharpCode = decompiler.DecompileAsString(handle);

        string ilCode;
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = new DefaultAssemblyResolver()
        };

        var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
        var member = assembly.MainModule.LookupToken(token);

        switch (member)
        {
            case MethodDefinition method when method.HasBody:
                var sb = new StringBuilder();
                foreach (var instruction in method.Body.Instructions)
                {
                    sb.AppendLine(instruction.ToString());
                }
                ilCode = sb.ToString();
                break;
            case MethodDefinition _:
                ilCode = "This method does not have a method body (e.g., it's abstract or in an interface).";
                break;
            default:
                ilCode = "IL code is available only for methods.";
                break;
        }

        var response = new DecompiledCodeDto
        {
            CSharpCode = csharpCode,
            IlCode = ilCode
        };

        return Task.FromResult(response);
    }
}
