using DotnetVoyager.BLL.Dtos;
using DotnetVoyager.BLL.Enums;
using Mono.Cecil;

namespace DotnetVoyager.BLL.Services;

public interface IStructureAnalyzerService
{
    Task<StructureNodeDto> AnalyzeStructureAsync(string assemblyPath);
}

public class StructureAnalyzerService : IStructureAnalyzerService
{
    private const string NoNamespace = "[No Namespace]";

    // Assembly does not have a metadata token, so we use a constant value
    private const int AssemblyToken = 0;

    // Namespace does not have a metadata token, so we use a constant value
    private const int NamespaceToken = 0;

    public Task<StructureNodeDto> AnalyzeStructureAsync(string assemblyPath)
    {
        return Task.Run(() =>
        {
            using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            return AnalyzeAssembly(assembly);
        });
    }

    private StructureNodeDto AnalyzeAssembly(AssemblyDefinition assembly)
    {
        var namespaceNodes = assembly.MainModule.Types
            .Where(t => t.IsPublic)
            .GroupBy(t => t.Namespace ?? NoNamespace)
            .Select(AnalyzeNamespace)
            .OrderBy(n => n.Name)
            .ToList();

        return new StructureNodeDto
        {
            Name = assembly.Name.Name,
            Type = StructureNodeType.Assembly,
            Token = AssemblyToken,
            Children = namespaceNodes.Any() ? namespaceNodes : null
        };
    }

    private StructureNodeDto AnalyzeNamespace(IGrouping<string, TypeDefinition> namespaceGroup)
    {
        var typeNodes = namespaceGroup
            .Select(AnalyzeTypeDefinition)
            .OrderBy(t => t.Name)
            .ToList();

        return new StructureNodeDto
        {
            Name = namespaceGroup.Key,
            Type = StructureNodeType.Namespace,
            Token = NamespaceToken,
            Children = typeNodes.Any() ? typeNodes : null
        };
    }

    private StructureNodeDto AnalyzeTypeDefinition(TypeDefinition type)
    {
        var methodNodes = type.Methods
            .Where(m => !m.IsConstructor && !m.IsSpecialName)
            .Select(m => new StructureNodeDto
            {
                Name = m.Name,
                Type = StructureNodeType.Method,
                Token = m.MetadataToken.ToInt32(),
                Children = null
            })
            .OrderBy(m => m.Name);

        var propertyNodes = type.Properties
            .Select(p => new StructureNodeDto
            {
                Name = p.Name,
                Type = StructureNodeType.Property,
                Token = p.MetadataToken.ToInt32(),
                Children = null
            })
            .OrderBy(p => p.Name);

        var children = propertyNodes.Concat(methodNodes).ToList();

        return new StructureNodeDto
        {
            Name = type.Name,
            Type = GetNodeType(type),
            Token = type.MetadataToken.ToInt32(),
            Children = children.Any() ? children : null
        };
    }

    private static StructureNodeType GetNodeType(TypeDefinition type)
    {
        if (type.IsInterface)
            return StructureNodeType.Interface;

        if (type.IsValueType)
            return StructureNodeType.Struct;

        if (type.IsClass)
            return StructureNodeType.Class;

        return StructureNodeType.Class;
    }
}
