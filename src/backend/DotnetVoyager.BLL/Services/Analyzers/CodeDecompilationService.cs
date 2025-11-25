using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Factories;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface ICodeDecompilationService
{
    Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token);
}

public class CodeDecompilationService : ICodeDecompilationService
{
    // We don't need the factory here if we want full control over the PEFile lifecycle 
    // to prevent double-loading. But we can reuse the logic if we refactor slightly.
    // Ideally, for performance, we open the file ONCE.

    public Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token)
    {
        return Task.Run(() =>
        {
            // 1. Open file once (FileShare.Read | FileShare.Delete is critical for your cleanup service)
            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var peFile = new PEFile(assemblyPath, stream);

            // 2. Setup Decompiler (reuse logic similar to your Factory, but here we need access to peFile directly)
            var resolver = new UniversalAssemblyResolver(assemblyPath, false, peFile.DetectTargetFrameworkId());
            var decompiler = new CSharpDecompiler(peFile, resolver, new DecompilerSettings());

            // 3. Decompile C#
            var handle = MetadataTokens.EntityHandle(token);
            var csharpCode = decompiler.DecompileAsString(handle);

            // 4. Disassemble IL (Clean way using built-in tools)
            string ilCode = GetIlCode(peFile, handle, CancellationToken.None);

            return new DecompiledCodeDto
            {
                CSharpCode = csharpCode,
                IlCode = ilCode
            };
        });
    }

    private static string GetIlCode(PEFile peFile, EntityHandle handle, CancellationToken token)
    {
        if (handle.Kind != HandleKind.MethodDefinition)
        {
            return "IL code is available only for methods.";
        }

        var output = new PlainTextOutput();

        // ReflectionDisassembler is the tool ILSpy uses internally to show IL view.
        // It works on top of PEFile, so no extra heavy parsing needed.
        var disassembler = new ReflectionDisassembler(output, token);

        // This generates standard, readable IL (ldstr, call, ret, etc.)
        disassembler.DisassembleMethod(peFile, (MethodDefinitionHandle)handle);

        return output.ToString();
    }
}

/*public class CodeDecompilationService(IDecompilerFactory decompilerFactory) : ICodeDecompilationService
{
    private readonly IDecompilerFactory _decompilerFactory = decompilerFactory;

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

        using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
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
}*/
