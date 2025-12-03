using DotnetVoyager.BLL.Dtos.AnalysisResults;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace DotnetVoyager.BLL.Services.Analyzers;

/// <summary>
/// Analyzes .NET assembly metadata and builds a hierarchical tree structure
/// of namespaces, types, methods, and properties using System.Reflection.Metadata API.
/// </summary>
public interface IAssemblyTreeAnalyzer
{
    Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath);
}

/// <summary>
/// Implementation that processes assembly metadata and filters out compiler-generated artifacts
/// to show only user-defined code structure.
/// </summary>
public class AssemblyTreeAnalyzer : IAssemblyTreeAnalyzer
{
    private const string NoNamespace = "[No Namespace]";
    private const int AssemblyToken = 0;
    private const int NamespaceToken = 0;

    public Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath)
    {
        return Task.Run(() =>
        {
            // Open the assembly file with shared read access to allow other processes to access it
            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var peReader = new PEReader(stream);

            // Verify that the file contains valid CLI metadata
            if (!peReader.HasMetadata)
            {
                throw new InvalidOperationException("The provided file does not contain CLI metadata.");
            }

            var reader = peReader.GetMetadataReader();
            // Dictionary to group types by their namespace for hierarchical organization
            var namespaceGroups = new Dictionary<string, List<AssemblyTreeNodeDto>>();

            // Iterate through all type definitions in the assembly metadata
            foreach (var typeHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeHandle);

                // FILTER: Skip compiler-generated types (closures, anonymous types, state machines, etc.)
                // This keeps the tree clean and shows only user-defined types
                if (IsCompilerGenerated(reader, typeDef))
                    continue;

                string ns = GetNamespace(reader, typeDef);

                // Create namespace group if it doesn't exist yet
                if (!namespaceGroups.TryGetValue(ns, out var list))
                {
                    list = new List<AssemblyTreeNodeDto>();
                    namespaceGroups.Add(ns, list);
                }

                // Add the type node to its namespace group
                list.Add(CreateTypeNode(reader, typeDef, typeHandle));
            }

            // Build the namespace nodes with sorted children
            var rootChildren = new List<AssemblyTreeNodeDto>(namespaceGroups.Count);

            foreach (var kvp in namespaceGroups)
            {
                // Sort types alphabetically within each namespace for better readability
                kvp.Value.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

                rootChildren.Add(new AssemblyTreeNodeDto
                {
                    Name = kvp.Key,
                    Type = AssemblyTreeNodeType.Namespace,
                    Token = NamespaceToken,
                    Children = kvp.Value
                });
            }

            // Sort namespaces alphabetically at the root level
            rootChildren.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            // Get the assembly name from metadata to use as the root node name
            var assemblyDef = reader.GetAssemblyDefinition();
            string assemblyName = reader.GetString(assemblyDef.Name);

            // Return the complete tree structure with assembly as root
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

        // Process all methods defined in the type
        foreach (var methodHandle in typeDef.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);

            // Filter out special methods like property getters/setters and constructors
            // SpecialName marks compiler-generated accessor methods
            // RTSpecialName marks runtime special methods like constructors
            var attributes = method.Attributes;
            if ((attributes & MethodAttributes.SpecialName) != 0 ||
                (attributes & MethodAttributes.RTSpecialName) != 0)
                continue;

            // ADDITIONAL FILTER: Skip compiler-generated methods (lambdas, local functions, etc.)
            string methodName = reader.GetString(method.Name);
            if (IsCompilerGeneratedName(methodName))
                continue;

            // Add the method as a leaf node (no children)
            children.Add(new AssemblyTreeNodeDto
            {
                Name = methodName,
                Type = AssemblyTreeNodeType.Method,
                Token = MetadataTokens.GetToken(methodHandle),
                Children = null
            });
        }

        // Process all properties defined in the type
        foreach (var propHandle in typeDef.GetProperties())
        {
            var prop = reader.GetPropertyDefinition(propHandle);
            string propName = reader.GetString(prop.Name);

            // FILTER: Skip compiler-generated properties (backing fields, etc.)
            if (IsCompilerGeneratedName(propName))
                continue;

            // Add the property as a leaf node
            children.Add(new AssemblyTreeNodeDto
            {
                Name = propName,
                Type = AssemblyTreeNodeType.Property,
                Token = MetadataTokens.GetToken(propHandle),
                Children = null
            });
        }

        // Sort members alphabetically for consistent display
        children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

        // Return the complete type node with all its members
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

        // Check typical patterns of compiler-generated type names
        if (IsCompilerGeneratedName(typeName))
            return true;

        // Additional check through attributes - look for CompilerGeneratedAttribute
        // This is more reliable but slower than name pattern matching
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

    // Checks if a name matches compiler-generated naming patterns.
    // The C# compiler uses angle brackets (< >) which are invalid in source code but allowed in IL:
    // - Anonymous types: <>f__AnonymousType
    // - Closure classes: <>c__DisplayClass
    // - Lambda cache: <>c
    // - Iterator/async state machines: <MethodName>d__
    private static bool IsCompilerGeneratedName(string name)
    {
        // Compiler-generated types and members have characteristic names:
        // - Start with '<' or '<>'
        // - Contain '>' in the name
        // - Anonymous types: <>f__AnonymousType
        // - Closure classes: <>c__DisplayClass
        // - Lambda cache: <>c
        // - Iterator/async state machines: <MethodName>d__

        if (string.IsNullOrEmpty(name))
            return false;

        // Angle brackets are not valid in C# identifiers, so their presence indicates compiler generation
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
        // Check the Interface flag in type attributes
        if ((attributes & TypeAttributes.Interface) != 0)
            return AssemblyTreeNodeType.Interface;

        // Default to Class for all other types (classes, structs, enums, delegates)
        return AssemblyTreeNodeType.Class;
    }
}