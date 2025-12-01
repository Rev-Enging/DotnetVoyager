using DotnetVoyager.BLL.Dtos.AnalysisResults;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System.Reflection.Metadata.Ecma335;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IInheritanceGraphBuilderService
{
    Task<InheritanceGraphDto> BuildGraphAsync(string assemblyPath);
}

public class InheritanceGraphBuilderService : IInheritanceGraphBuilderService
{
    public Task<InheritanceGraphDto> BuildGraphAsync(string assemblyPath)
    {
        // Open the file stream with shared access to avoid locking errors.
        using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var peFile = new PEFile(assemblyPath, stream);

        // Create a resolver to find external dependencies (like System.dll).
        var resolver = new UniversalAssemblyResolver(assemblyPath, false, peFile.DetectTargetFrameworkId());

        // DecompilerTypeSystem parses the binary metadata into a high-level class hierarchy.
        var typeSystem = new DecompilerTypeSystem(peFile, resolver);

        var builder = new GraphBuilder(typeSystem.MainModule);
        var edges = new List<InheritanceGraphEdgeDto>();

        foreach (var typeDef in typeSystem.MainModule.TypeDefinitions)
        {
            // Skip compiler-generated types (e.g., async state machines) to keep the graph readable.
            if (typeDef.IsCompilerGenerated()) continue;

            var sourceId = builder.GetOrCreateNode(typeDef);

            // Iterate over the parent class and implemented interfaces.
            foreach (var baseType in typeDef.DirectBaseTypes)
            {
                // Exclude System.Object to prevent the graph from becoming a giant web 
                // (since almost everything inherits from Object).
                if (baseType.IsKnownType(KnownTypeCode.Object)) continue;

                var targetId = builder.GetOrCreateNode(baseType);

                edges.Add(new InheritanceGraphEdgeDto
                {
                    Id = $"{sourceId}_{targetId}",
                    Source = sourceId,
                    Target = targetId
                });
            }
        }

        var result = new InheritanceGraphDto
        {
            Nodes = builder.GetNodes(),
            Edges = edges
        };

        return Task.FromResult(result);
    }

    private sealed class GraphBuilder
    {
        private readonly Dictionary<string, InheritanceGraphNodeDto> _nodes = new();
        private readonly IModule _mainModule;

        public GraphBuilder(IModule mainModule)
        {
            _mainModule = mainModule;
        }

        public List<InheritanceGraphNodeDto> GetNodes()
            => _nodes.Values.OrderBy(n => n.ShortName).ToList();

        private bool IsExternal(ITypeDefinition def)
        {
            // A type is external if it belongs to a module different from the one currently being analyzed.
            return def.ParentModule != null && def.ParentModule != _mainModule;
        }

        public string GetOrCreateNode(IType type)
        {
            // Unwrap generics or pointers to get the underlying type definition.
            var definition = type.GetDefinition();

            string id;
            int tokenId = 0;
            bool isExternal = true;

            if (definition == null)
            {
                // If definition is null, the type is likely a generic parameter or unresolved external type.
                id = type.FullName;
            }
            else
            {
                isExternal = IsExternal(definition);

                if (isExternal)
                {
                    // For external types (e.g., System.String), use the full name as ID.
                    id = definition.FullName;
                }
                else
                {
                    // For internal types, use the MetadataToken (unique integer ID in the assembly).
                    // This is safer than strings for internal linking.
                    tokenId = MetadataTokens.GetToken(definition.MetadataToken);
                    id = tokenId.ToString();
                }
            }

            if (_nodes.TryGetValue(id, out var existing)) return existing.Id;

            var newNode = new InheritanceGraphNodeDto
            {
                Id = id,
                TokenId = tokenId,
                FullName = type.FullName,
                ShortName = type.Name,
                Type = GetGraphNodeType(type.Kind),
                IsExternal = isExternal
            };

            _nodes.Add(id, newNode);
            return id;
        }

        private static InheritanceGraphNodeType GetGraphNodeType(TypeKind kind) => kind switch
        {
            TypeKind.Interface => InheritanceGraphNodeType.Interface,
            TypeKind.Struct => InheritanceGraphNodeType.Struct,
            TypeKind.Enum => InheritanceGraphNodeType.Enum,
            _ => InheritanceGraphNodeType.Class
        };
    }
}
