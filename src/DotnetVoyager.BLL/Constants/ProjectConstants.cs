namespace DotnetVoyager.BLL.Constants;

public static class ProjectConstants
{
    public const int MaxAssemblySizeInMegabytes = 3;
    public const int MaxAssemblySizeInBytes = MaxAssemblySizeInMegabytes * 1024 * 1024;

    public static readonly string CorsOptionsSectionName = "CorsOptions";

    public static readonly string StorageSettingsSectionName = "StorageOptions";
    public static readonly string DefaultAnalysisStoragePath = "AnalysisFiles";
    public static readonly int DefaultFileLifetimeMinutes = 60;

    public static readonly string WorkerSettingsSectionName = "WorkerSettings";
    public static readonly int DefaultAnalysisConcurrentWorkers = 5;
    public static readonly int DefaultAnalysisTimeoutMinutes = 10;

    public static readonly string NamespaceTreeStructureFileName = "namespace-tree.json";
}
