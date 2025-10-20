using DotnetVoyager.BLL.Constants;

namespace DotnetVoyager.BLL.Options;

public class StorageOptions
{
    public string Path { get; init; } = ProjectConstants.DefaultAnalysisStoragePath;
    public int FileLifetimeMinutes { get; init; } = ProjectConstants.DefaultFileLifetimeMinutes;
    public int CleanupIntervalMinutes { get; init; } = ProjectConstants.DefaultCleanupIntervalMinutes;
}
