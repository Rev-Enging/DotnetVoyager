using DotnetVoyager.BLL.Dtos;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;
using Mono.Cecil;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DotnetVoyager.BLL.Services;

public interface ICodeDecompilationService
{
    Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token);
}

public class CodeDecompilationService : ICodeDecompilationService
{
    public Task<DecompiledCodeDto> DecompileCodeAsync(string assemblyPath, int token)
    {
        var handle = MetadataTokens.EntityHandle(token);

        // Налаштовуємо resolver для роботи без залежностей
        var resolver = new UniversalAssemblyResolver(
            assemblyPath,
            throwOnError: false,  // Не кидати виняток при відсутності залежностей
            targetFramework: null
        );

        // Додаємо директорію з поточною збіркою
        resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

        // Налаштовуємо DecompilerSettings
        var settings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false  // Не падати при помилках резолвінгу
        };

        // Створюємо декомпілятор з налаштованим resolver
        var decompiler = new CSharpDecompiler(assemblyPath, resolver, settings);

        var csharpCode = decompiler.DecompileAsString(handle);

        // IL-код через Mono.Cecil
        string ilCode;
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = new DefaultAssemblyResolver() // Cecil теж потребує resolver
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
