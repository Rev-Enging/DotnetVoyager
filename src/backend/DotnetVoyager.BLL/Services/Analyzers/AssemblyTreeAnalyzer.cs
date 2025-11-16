using DotnetVoyager.BLL.Dtos.AnalysisResults;
using Mono.Cecil;

namespace DotnetVoyager.BLL.Services.Analyzers;

public interface IAssemblyTreeAnalyzer
{
    Task<AssemblyTreeDto> AnalyzeAssemblyTreeAsync(string assemblyPath);
}

public class AssemblyTreeAnalyzer : IAssemblyTreeAnalyzer
{
    private const string NoNamespace = "[No Namespace]";

    // Assembly does not have a metadata token, so we use a constant value
    private const int AssemblyToken = 0;

    // Namespace does not have a metadata token, so we use a constant value
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
