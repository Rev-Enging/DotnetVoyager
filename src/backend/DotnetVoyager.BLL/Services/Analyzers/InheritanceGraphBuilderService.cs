using DotnetVoyager.BLL.Dtos.AnalysisResults;
using DotnetVoyager.BLL.Factories;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem.Implementation;
using System.Reflection.Metadata.Ecma335;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IInheritanceGraphBuilderService
{
    Task<InheritanceGraphDto> BuildGraphAsync(string assemblyPath);
}

/// <summary>
/// A stateless service responsible for building the inheritance graph.
/// It uses a stateful, short-lived GraphBuilder for each operation.
/// </summary>
public class InheritanceGraphBuilderService : IInheritanceGraphBuilderService
{
    private readonly IDecompilerFactory _decompilerFactory;

    public InheritanceGraphBuilderService(IDecompilerFactory decompilerFactory)
    {
        _decompilerFactory = decompilerFactory;
    }

    public Task<InheritanceGraphDto> BuildGraphAsync(string assemblyPath)
    {
        var decompiler = _decompilerFactory.Create(assemblyPath);
        var typeSystem = decompiler.TypeSystem;
        var edges = new List<InheritanceGraphEdgeDto>();

        // Create a stateful builder for this specific build operation.
        // This keeps the service (InheritanceGraphService) stateless.
        var builder = new GraphBuilder(typeSystem.MainModule.AssemblyName);

        foreach (var typeDef in typeSystem.MainModule.TypeDefinitions)
        {
            if (typeDef.IsCompilerGenerated())
                continue;

            // GetOrCreateNode(IType) will safely accept an ITypeDefinition
            var sourceId = builder.GetOrCreateNode(typeDef);

            // Process all base types and interfaces
            foreach (var baseType in typeDef.DirectBaseTypes)
            {
                // Skip System.Object for graph clarity
                if (baseType.FullName == "System.Object")
                    continue;

                var targetId = builder.GetOrCreateNode(baseType);

                edges.Add(new InheritanceGraphEdgeDto
                {
                    Id = $"{sourceId}_to_{targetId}",
                    Source = sourceId,
                    Target = targetId
                });
            }
        }

        var graph = new InheritanceGraphDto
        {
            // Get the final node list from the builder
            Nodes = builder.GetNodes(),
            Edges = edges
        };

        return Task.FromResult(graph);
    }

    /// <summary>
    /// A private helper class to manage the state of a single graph build.
    /// This encapsulates the node dictionary and assembly name.
    /// </summary>
    private sealed class GraphBuilder
    {
        private readonly Dictionary<string, InheritanceGraphNodeDto> _nodes = new();
        private readonly string _mainAssemblyName;

        public GraphBuilder(string mainAssemblyName)
        {
            _mainAssemblyName = mainAssemblyName;
        }

        /// <summary>
        /// Returns the finalized list of all unique nodes found.
        /// </summary>
        public List<InheritanceGraphNodeDto> GetNodes()
        {
            return _nodes.Values.OrderBy(n => n.ShortName).ToList();
        }

        /// <summary>
        /// Checks if a type is external (not defined in the current assembly).
        /// </summary>
        public bool IsExternal(IType type)
        {
            switch (type.Kind)
            {
                case TypeKind.Array:
                case TypeKind.Pointer:
                case TypeKind.ByReference:
                    // For wrapper types (arrays, pointers), check the element type
                    if (type is TypeWithElementType elementType)
                    {
                        return IsExternal(elementType.ElementType);
                    }
                    return true;

                case TypeKind.TypeParameter:
                    // Type parameters (T, TKey, etc.) are not external
                    return false;

                default:
                    var definition = type.GetDefinition();
                    if (definition != null)
                    {
                        // Safely check for null ParentModule
                        if (definition.ParentModule == null)
                        {
                            return true;
                        }
                        return definition.ParentModule.AssemblyName != _mainAssemblyName;
                    }
                    // If we couldn't find the definition, consider it external
                    return true;
            }
        }

        /// <summary>
        /// Gets a unique ID for a node and creates the node if it doesn't exist.
        /// Safely handles any IType.
        /// </summary>
        public string GetOrCreateNode(IType type)
        {
            bool isExternal = IsExternal(type);
            string id;
            int tokenId;

            if (isExternal)
            {
                // External types: ID = FullName, TokenId = 0
                id = type.FullName;
                tokenId = 0;
            }
            else
            {
                // Internal types: ID = Token, TokenId = Token
                var definition = type.GetDefinition();

                if (definition == null)
                {
                    // Fallback for an edge case
                    id = type.FullName;
                    tokenId = 0;
                    isExternal = true;
                }
                else
                {
                    // Get metadata token from definition
                    tokenId = MetadataTokens.GetToken(definition.MetadataToken);
                    id = tokenId.ToString();
                }
            }

            // If the node already exists, return its ID
            if (_nodes.ContainsKey(id))
            {
                return id;
            }

            // Create a new node
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

        /// <summary>
        /// Determines the graph node type based on TypeKind.
        /// </summary>
        private static InheritanceGraphNodeType GetGraphNodeType(TypeKind kind)
        {
            return kind switch
            {
                TypeKind.Interface => InheritanceGraphNodeType.Interface,
                TypeKind.Struct => InheritanceGraphNodeType.Struct,
                _ => InheritanceGraphNodeType.Class
            };
        }
    }
}