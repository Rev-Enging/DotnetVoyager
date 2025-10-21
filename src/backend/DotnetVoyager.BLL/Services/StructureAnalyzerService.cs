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

    public Task<StructureNodeDto> AnalyzeStructureAsync(string assemblyPath)
    {
        // Робота з Mono.Cecil є синхронною і може бути CPU/IO-інтенсивною.
        // Ми переносимо її в фоновий потік, щоб не блокувати викликаючий потік (напр. UI або API).
        return Task.Run(() =>
        {
            // 'using' критично важливий для звільнення файлу збірки!
            using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
            return AnalyzeAssembly(assembly);
        });
    }

    /// <summary>
    /// Створює кореневий вузол для самої збірки.
    /// </summary>
    private StructureNodeDto AnalyzeAssembly(AssemblyDefinition assembly)
    {
        var namespaceNodes = assembly.MainModule.Types
            .Where(t => t.IsPublic) // Беремо тільки публічні типи
            .GroupBy(t => t.Namespace ?? NoNamespace)
            .Select(AnalyzeNamespace) // Використовуємо чистий метод-трансформер
            .OrderBy(n => n.Name)
            .ToList();

        return new StructureNodeDto
        {
            Name = assembly.Name.Name,
            Type = StructureNodeType.Assembly,
            Token = 0, // Збірка не має релевантного токена в цьому контексті
            Children = namespaceNodes.Any() ? namespaceNodes : null
        };
    }

    /// <summary>
    /// Створює вузол для простору імен з його дочірніми типами.
    /// </summary>
    private StructureNodeDto AnalyzeNamespace(IGrouping<string, TypeDefinition> namespaceGroup)
    {
        var typeNodes = namespaceGroup
            .Select(AnalyzeTypeDefinition) // Делегуємо аналіз типу
            .OrderBy(t => t.Name)
            .ToList();

        return new StructureNodeDto
        {
            Name = namespaceGroup.Key,
            Type = StructureNodeType.Namespace,
            Token = 0, // Простір імен не має токена
            Children = typeNodes.Any() ? typeNodes : null
        };
    }

    /// <summary>
    /// Створює вузол для типу (клас, інтерфейс, структура).
    /// </summary>
    private StructureNodeDto AnalyzeTypeDefinition(TypeDefinition type)
    {
        // Збираємо публічні методи (крім конструкторів)
        var methodNodes = type.Methods
            .Where(m => m.IsPublic && !m.IsConstructor && !m.IsSpecialName) // !IsSpecialName приховає get/set
            .Select(m => new StructureNodeDto
            {
                Name = m.Name,
                Type = StructureNodeType.Method,
                Token = m.MetadataToken.ToInt32(),
                Children = null // Методи - кінцеві вузли
            })
            .OrderBy(m => m.Name);

        // (Бонус) Збираємо публічні властивості
        var propertyNodes = type.Properties
            .Where(p => (p.GetMethod?.IsPublic ?? false) || (p.SetMethod?.IsPublic ?? false))
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
            Type = GetNodeType(type), // Використовуємо enum
            Token = type.MetadataToken.ToInt32(),
            Children = children.Any() ? children : null
        };
    }

    private static StructureNodeType GetNodeType(TypeDefinition type)
    {
        if (type.IsInterface)
            return StructureNodeType.Interface;

        if (type.IsValueType) // Це включає struct
            return StructureNodeType.Struct;

        if (type.IsClass)
            return StructureNodeType.Class;

        // За замовчуванням
        return StructureNodeType.Class;
    }
}
