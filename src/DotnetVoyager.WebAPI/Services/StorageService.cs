using DotnetVoyager.WebAPI.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DotnetVoyager.WebAPI.Services;

public interface IStorageService
{
    /// <summary>
    /// Отримує повний шлях до папки для конкретної сесії аналізу.
    /// </summary>
    string GetAnalysisDirectoryPath(string analysisId);

    /// <summary>
    /// Асинхронно зберігає файли в папку для конкретної сесії аналізу.
    /// </summary>
    /// <returns>Повний шлях до створеної папки.</returns>
    Task<string> SaveAnalysisFilesAsync(List<IFormFile> files, string analysisId);
}

public class StorageService : IStorageService
{
    private readonly StorageOptions _options;
    private readonly string _contentRootPath; // Будемо використовувати ContentRootPath для відносних шляхів

    public StorageService(IOptions<StorageOptions> options, IWebHostEnvironment webHostEnvironment)
    {
        _options = options.Value;
        _contentRootPath = webHostEnvironment.ContentRootPath;
    }

    public string GetAnalysisDirectoryPath(string analysisId)
    {
        string basePath = _options.Type switch
        {
            // Для WwwRoot ми все ще використовуємо WebRootPath
            StorageType.WwwRoot => Path.Combine(Path.Combine(_contentRootPath, "wwwroot"), "uploads"),

            StorageType.Custom => GetCustomBasePath(),

            StorageType.Temp => Path.Combine(Path.GetTempPath(), "DotnetVoyager"),

            _ => throw new InvalidOperationException("Invalid StorageType configured.")
        };

        return Path.Combine(basePath, analysisId);
    }

    public async Task<string> SaveAnalysisFilesAsync(List<IFormFile> files, string analysisId)
    {
        // 1. Отримуємо шлях до папки, де будуть зберігатися файли
        var targetDirectoryPath = GetAnalysisDirectoryPath(analysisId);

        // 2. Створюємо папку
        Directory.CreateDirectory(targetDirectoryPath);

        // 3. Зберігаємо файли (логіка переїхала сюди з контролера)
        foreach (var file in files)
        {
            if (file.Length > 0)
            {
                var filePath = Path.Combine(targetDirectoryPath, file.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
        }

        return targetDirectoryPath;
    }

    // Приватний метод для обробки логіки CustomPath
    private string GetCustomBasePath()
    {
        var customPath = _options.CustomPath;

        if (string.IsNullOrEmpty(customPath))
        {
            throw new InvalidOperationException("CustomPath must be configured when StorageType is Custom.");
        }

        // Перевіряємо, чи шлях абсолютний (наприклад, "C:\uploads" або "/var/uploads")
        if (Path.IsPathRooted(customPath))
        {
            return customPath;
        }

        // Якщо шлях відносний (наприклад, "MyUploads"), поєднуємо його з кореневою папкою проєкту
        return Path.Combine(_contentRootPath, customPath);
    }
}
