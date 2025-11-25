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
            // Optimization: Use FileStream with FileShare.Read | FileShare.Delete.
            // This prevents loading the entire file into RAM and avoids locking the file 
            // against the background cleanup service.
            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var peReader = new PEReader(stream);

            if (!peReader.HasMetadata)
            {
                // In a real scenario, you might want to return an error DTO or throw a custom exception
                throw new InvalidOperationException("The provided file does not contain CLI metadata.");
            }

            var reader = peReader.GetMetadataReader();

            // Optimization: Use Dictionary for grouping instead of LINQ GroupBy to reduce memory allocations.
            var namespaceGroups = new Dictionary<string, List<AssemblyTreeNodeDto>>();

            // Iterate over all type definitions using handles
            foreach (var typeHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeHandle);

                // Determine namespace
                string ns = GetNamespace(reader, typeDef);

                if (!namespaceGroups.TryGetValue(ns, out var list))
                {
                    list = new List<AssemblyTreeNodeDto>();
                    namespaceGroups.Add(ns, list);
                }

                // FIX: Pass the typeHandle explicitly to avoid the "missing Handle property" error
                list.Add(CreateTypeNode(reader, typeDef, typeHandle));
            }

            // Construct the final tree structure
            var rootChildren = new List<AssemblyTreeNodeDto>(namespaceGroups.Count);

            foreach (var kvp in namespaceGroups)
            {
                // Sort types within the namespace alphabetically
                kvp.Value.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

                rootChildren.Add(new AssemblyTreeNodeDto
                {
                    Name = kvp.Key,
                    Type = AssemblyTreeNodeType.Namespace,
                    Token = NamespaceToken,
                    Children = kvp.Value
                });
            }

            // Sort namespaces alphabetically
            rootChildren.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            // Get Assembly Name safely
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

        // 1. Process Methods
        foreach (var methodHandle in typeDef.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);

            // Filter: Skip special methods (getters, setters, constructors) to keep the tree clean.
            // If you want to see constructors, remove the check for RTSpecialName.
            var attributes = method.Attributes;
            if ((attributes & MethodAttributes.SpecialName) != 0 ||
                (attributes & MethodAttributes.RTSpecialName) != 0)
                continue;

            children.Add(new AssemblyTreeNodeDto
            {
                Name = reader.GetString(method.Name),
                Type = AssemblyTreeNodeType.Method,
                Token = MetadataTokens.GetToken(methodHandle),
                Children = null
            });
        }

        // 2. Process Properties
        foreach (var propHandle in typeDef.GetProperties())
        {
            var prop = reader.GetPropertyDefinition(propHandle);
            children.Add(new AssemblyTreeNodeDto
            {
                Name = reader.GetString(prop.Name),
                Type = AssemblyTreeNodeType.Property,
                Token = MetadataTokens.GetToken(propHandle),
                Children = null
            });
        }

        // Sort members (methods and properties mixed) alphabetically
        children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        return new AssemblyTreeNodeDto
        {
            Name = reader.GetString(typeDef.Name),
            Type = GetNodeType(typeDef.Attributes),
            // FIX: Use the explicitly passed handle converted to an integer token
            Token = MetadataTokens.GetToken(typeHandle),
            Children = children.Count > 0 ? children : null
        };
    }

    private static string GetNamespace(MetadataReader reader, TypeDefinition typeDef)
    {
        if (typeDef.Namespace.IsNil) return NoNamespace;
        return reader.GetString(typeDef.Namespace);
    }

    private static AssemblyTreeNodeType GetNodeType(TypeAttributes attributes)
    {
        // Check for Interface
        if ((attributes & TypeAttributes.Interface) != 0)
            return AssemblyTreeNodeType.Interface;

        // Note: Determining strictly if a type is a "Struct" (ValueType) using only MetadataReader 
        // without resolving references is complex because we need to check the BaseType.
        // For a high-performance tree view, assuming Class (or checking Sealed + SequentialLayout as a heuristic) 
        // is usually sufficient.

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