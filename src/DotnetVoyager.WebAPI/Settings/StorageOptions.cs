using DotnetVoyager.WebAPI.Constants;

namespace DotnetVoyager.WebAPI.Configuration;

public class StorageOptions
{
    public string Path { get; init; } = ProjectConstants.DefaultAnalysisStoragePath;
    public int FileLifetimeMinutes { get; init; } = ProjectConstants.DefaultFileLifetimeMinutes;
}
