namespace DotnetVoyager.WebAPI.Configuration;

public enum StorageType
{
    WwwRoot,
    Custom,
    Temp
}

public class StorageOptions
{
    // Вказує, який тип сховища використовувати
    public StorageType Type { get; set; } = StorageType.WwwRoot; // WwwRoot за замовчуванням

    // Шлях до кастомної папки (використовується, тільки якщо Type = Custom)
    public string? CustomPath { get; set; }
}
