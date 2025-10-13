using DotnetVoyager.WebAPI.Configuration;
using DotnetVoyager.WebAPI.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DotnetVoyager.WebAPI.Services;

public interface IStorageService
{
    /// <summary>
    /// Отримує шлях до папки аналізу.
    /// </summary>
    string GetAnalysisDirectoryPath(string analysisId);

    /// <summary>
    /// Зберігає завантажений файл збірки.
    /// </summary>
    Task SaveAssemblyFileAsync(IFormFile file, string analysisId);

    // === Робота з результатами аналізу ===

    /// <summary>
    /// Зберігає серіалізоване дерево структури збірки у JSON-файл.
    /// </summary>
    Task SaveTreeAsync(AssemblyNodeDto node, string analysisId);

    /// <summary>
    /// Зчитує та десеріалізує дерево структури збірки з JSON-файлу.
    /// Повертає null, якщо файл не знайдено.
    /// </summary>
    Task<AssemblyNodeDto?> ReadTreeAsync(string analysisId);

    // === Управління життєвим циклом ===

    /// <summary>
    /// Повністю видаляє папку з усіма файлами аналізу.
    /// </summary>
    Task DeleteAnalysisAsync(string analysisId);
}

public class StorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly string _contentRootPath;

    public StorageService(IOptions<StorageOptions> options, IWebHostEnvironment webHostEnvironment)
    {
        _options = options.Value;
        _contentRootPath = webHostEnvironment.ContentRootPath;
    }

    // Метод тепер значно простіший
    public string GetAnalysisDirectoryPath(string analysisId)
    {
        // 1. Отримуємо базовий шлях з налаштувань.
        var basePath = _options.Path;

        // 2. Якщо шлях відносний (напр., "AnalysisTemp"), поєднуємо його з коренем проєкту.
        // Якщо абсолютний (напр., "C:\\Uploads"), використовуємо як є.
        var absoluteBasePath = Path.IsPathRooted(basePath)
            ? basePath
            : Path.Combine(_contentRootPath, basePath);

        // 3. Додаємо унікальний ID аналізу
        return Path.Combine(absoluteBasePath, analysisId);
    }

    // Я трохи спростив метод, оскільки ми аналізуємо лише один файл за раз
    public async Task SaveAnalysisFilesAsync(IFormFile file, string analysisId)
    {
        // 1. Отримуємо фінальний шлях до папки.
        var targetDirectoryPath = GetAnalysisDirectoryPath(analysisId);

        // 2. Створюємо папку, якщо її ще немає.
        Directory.CreateDirectory(targetDirectoryPath);

        // 3. Зберігаємо файл.
        var filePath = Path.Combine(targetDirectoryPath, file.FileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
    }

    public async Task SaveTreeAsync(AssemblyNodeDto node, string analysisId)
    {
        // 1. Отримуємо шлях до папки
        var directoryPath = GetAnalysisDirectoryPath(analysisId);
        // 2. Формуємо повний шлях до JSON-файлу, використовуючи константу
        var filePath = Path.Combine(directoryPath, ProjectConstants.NamespaceTreeStructureFileName);

        // 3. Асинхронно серіалізуємо об'єкт і записуємо у файл
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await System.Text.Json.JsonSerializer.SerializeAsync(fileStream, node);
    }

    public async Task<AssemblyNodeDto?> ReadTreeAsync(string analysisId)
    {
        var filePath = Path.Combine(GetAnalysisDirectoryPath(analysisId), ProjectConstants.NamespaceTreeStructureFileName);

        // ✅ Обробка ситуації, коли файл не існує
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var fileStream = new FileStream(filePath, FileMode.Open);
        // Десеріалізуємо JSON назад в об'єкт
        return await System.Text.Json.JsonSerializer.DeserializeAsync<AssemblyNodeDto>(fileStream);
    }

    public Task DeleteAnalysisAsync(string analysisId)
    {
        var directoryPath = GetAnalysisDirectoryPath(analysisId);

        // ✅ Обробка ситуації, коли папка не існує
        if (Directory.Exists(directoryPath))
        {
            // Видаляємо папку рекурсивно з усім вмістом
            Directory.Delete(directoryPath, recursive: true);
        }

        // Оскільки Directory.Delete - синхронна операція,
        // повертаємо завершене завдання, щоб відповідати інтерфейсу.
        return Task.CompletedTask;
    }
}
