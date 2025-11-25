using DotnetVoyager.BLL.Dtos.AnalysisResults;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IAssemblyTreeAnalyzer
{
    Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath);
}

public class AssemblyTreeAnalyzer : IAssemblyTreeAnalyzer
{
    private const string NoNamespace = "[No Namespace]";
    private const int AssemblyToken = 0;
    private const int NamespaceToken = 0;

    public Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath)
    {
        return Task.Run(() =>
        {
            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                throw new InvalidOperationException("The provided file does not contain CLI metadata.");
            }

            var reader = peReader.GetMetadataReader();
            var namespaceGroups = new Dictionary<string, List<AssemblyTreeNodeDto>>();

            foreach (var typeHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeHandle);

                // ФІЛЬТР: Пропускаємо compiler-generated типи
                if (IsCompilerGenerated(reader, typeDef))
                    continue;

                string ns = GetNamespace(reader, typeDef);

                if (!namespaceGroups.TryGetValue(ns, out var list))
                {
                    list = new List<AssemblyTreeNodeDto>();
                    namespaceGroups.Add(ns, list);
                }

                list.Add(CreateTypeNode(reader, typeDef, typeHandle));
            }

            var rootChildren = new List<AssemblyTreeNodeDto>(namespaceGroups.Count);

            foreach (var kvp in namespaceGroups)
            {
                kvp.Value.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

                rootChildren.Add(new AssemblyTreeNodeDto
                {
                    Name = kvp.Key,
                    Type = AssemblyTreeNodeType.Namespace,
                    Token = NamespaceToken,
                    Children = kvp.Value
                });
            }

            rootChildren.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            var assemblyDef = reader.GetAssemblyDefinition();
            string assemblyName = reader.GetString(assemblyDef.Name);

            return new AssemblyTreeDto
            {
                Root = new AssemblyTreeNodeDto
                {
                    Name = assemblyName,
                    Type = AssemblyTreeNodeType.Assembly,
                    Token = AssemblyToken,
                    Children = rootChildren
                }
            };
        });
    }

    private AssemblyTreeNodeDto CreateTypeNode(MetadataReader reader, TypeDefinition typeDef, TypeDefinitionHandle typeHandle)
    {
        var children = new List<AssemblyTreeNodeDto>();

        // Process Methods
        foreach (var methodHandle in typeDef.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);

            // Фільтруємо спеціальні методи та compiler-generated
            var attributes = method.Attributes;
            if ((attributes & MethodAttributes.SpecialName) != 0 ||
                (attributes & MethodAttributes.RTSpecialName) != 0)
                continue;

            // ДОДАТКОВИЙ ФІЛЬТР: пропускаємо compiler-generated методи
            string methodName = reader.GetString(method.Name);
            if (IsCompilerGeneratedName(methodName))
                continue;

            children.Add(new AssemblyTreeNodeDto
            {
                Name = methodName,
                Type = AssemblyTreeNodeType.Method,
                Token = MetadataTokens.GetToken(methodHandle),
                Children = null
            });
        }

        // Process Properties
        foreach (var propHandle in typeDef.GetProperties())
        {
            var prop = reader.GetPropertyDefinition(propHandle);
            string propName = reader.GetString(prop.Name);

            // ФІЛЬТР: пропускаємо compiler-generated властивості
            if (IsCompilerGeneratedName(propName))
                continue;

            children.Add(new AssemblyTreeNodeDto
            {
                Name = propName,
                Type = AssemblyTreeNodeType.Property,
                Token = MetadataTokens.GetToken(propHandle),
                Children = null
            });
        }

        children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        return new AssemblyTreeNodeDto
        {
            Name = reader.GetString(typeDef.Name),
            Type = GetNodeType(typeDef.Attributes),
            Token = MetadataTokens.GetToken(typeHandle),
            Children = children.Count > 0 ? children : null
        };
    }

    private static bool IsCompilerGenerated(MetadataReader reader, TypeDefinition typeDef)
    {
        string typeName = reader.GetString(typeDef.Name);

        // Перевіряємо типові патерни compiler-generated типів
        if (IsCompilerGeneratedName(typeName))
            return true;

        // Додаткова перевірка через атрибути
        foreach (var attrHandle in typeDef.GetCustomAttributes())
        {
            var attr = reader.GetCustomAttribute(attrHandle);
            var attrCtor = attr.Constructor;

            if (attrCtor.Kind == HandleKind.MemberReference)
            {
                var memberRef = reader.GetMemberReference((MemberReferenceHandle)attrCtor);
                var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
                string attrTypeName = reader.GetString(typeRef.Name);

                if (attrTypeName == "CompilerGeneratedAttribute")
                    return true;
            }
        }

        return false;
    }

    private static bool IsCompilerGeneratedName(string name)
    {
        // Compiler-generated типи та члени мають характерні імена:
        // - Починаються з '<' або '<>'
        // - Містять '>' в імені
        // - Анонімні типи: <>f__AnonymousType
        // - Closure класи: <>c__DisplayClass
        // - Lambda кеш: <>c
        // - Iterator/async state machines: <MethodName>d__

        if (string.IsNullOrEmpty(name))
            return false;

        return name.StartsWith("<>") ||
               (name.Contains('<') && name.Contains('>'));
    }

    private static string GetNamespace(MetadataReader reader, TypeDefinition typeDef)
    {
        if (typeDef.Namespace.IsNil) return NoNamespace;
        return reader.GetString(typeDef.Namespace);
    }

    private static AssemblyTreeNodeType GetNodeType(TypeAttributes attributes)
    {
        if ((attributes & TypeAttributes.Interface) != 0)
            return AssemblyTreeNodeType.Interface;

        return AssemblyTreeNodeType.Class;
    }
}

/*using DotnetVoyager.BLL.Dtos.AnalysisResults;
using Mono.Cecil;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IAssemblyTreeAnalyzer
{
    Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath);
}

public class AssemblyTreeAnalyzer : IAssemblyTreeAnalyzer
{
    private const string NoNamespace = "[No Namespace]";
    private const int AssemblyToken = 0;
    private const int NamespaceToken = 0;

    public Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath)
    {
        return Task.Run(() =>
        {
            using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);

            var rootNode = AnalyzeAssembly(assembly);

            return new AssemblyTreeDto
            {
                Root = rootNode
            };
        });
    }

    private AssemblyTreeNodeDto AnalyzeAssembly(AssemblyDefinition assembly)
    {
        var namespaceNodes = assembly.MainModule.Types
            .Where(t => t.IsPublic)
            .GroupBy(t => t.Namespace ?? NoNamespace)
            .Select(AnalyzeNamespace)
            .OrderBy(n => n.Name)
            .ToList();

        return new AssemblyTreeNodeDto
        {
            Name = assembly.Name.Name,
            Type = AssemblyTreeNodeType.Assembly,
            Token = AssemblyToken,
            Children = namespaceNodes.Any() ? namespaceNodes : null
        };
    }

    private AssemblyTreeNodeDto AnalyzeNamespace(IGrouping<string, TypeDefinition> namespaceGroup)
    {
        var typeNodes = namespaceGroup
            .Select(AnalyzeTypeDefinition)
            .OrderBy(t => t.Name)
            .ToList();

        return new AssemblyTreeNodeDto
        {
            Name = namespaceGroup.Key,
            Type = AssemblyTreeNodeType.Namespace,
            Token = NamespaceToken,
            Children = typeNodes.Any() ? typeNodes : null
        };
    }

    private AssemblyTreeNodeDto AnalyzeTypeDefinition(TypeDefinition type)
    {
        var methodNodes = type.Methods
            .Where(m => !m.IsConstructor && !m.IsSpecialName)
            .Select(m => new AssemblyTreeNodeDto
            {
                Name = m.Name,
                Type = AssemblyTreeNodeType.Method,
                Token = m.MetadataToken.ToInt32(),
                Children = null
            })
            .OrderBy(m => m.Name);

        var propertyNodes = type.Properties
            .Select(p => new AssemblyTreeNodeDto
            {
                Name = p.Name,
                Type = AssemblyTreeNodeType.Property,
                Token = p.MetadataToken.ToInt32(),
                Children = null
            })
            .OrderBy(p => p.Name);

        var children = propertyNodes.Concat(methodNodes).ToList();

        return new AssemblyTreeNodeDto
        {
            Name = type.Name,
            Type = GetNodeType(type),
            Token = type.MetadataToken.ToInt32(),
            Children = children.Any() ? children : null
        };
    }

    private static AssemblyTreeNodeType GetNodeType(TypeDefinition type)
    {
        if (type.IsInterface)
            return AssemblyTreeNodeType.Interface;

        if (type.IsValueType)
            return AssemblyTreeNodeType.Struct;

        if (type.IsClass)
            return AssemblyTreeNodeType.Class;

        return AssemblyTreeNodeType.Class;
    }
}
*/