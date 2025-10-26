namespace DotnetVoyager.BLL.Constants;

public static class ProjectConstants
{
    public const int MaxAssemblySizeInMegabytes = 3;
    public const int MaxAssemblySizeInBytes = MaxAssemblySizeInMegabytes * 1024 * 1024;

    public static readonly string CorsOptionsSectionName = "CorsOptions";

    public static readonly string AssemblyStorageSettingsSectionName = "StorageOptions";
    public static readonly string DefaultAnalysisStoragePath = "AnalysisFiles";
    public static readonly int DefaultFileLifetimeMinutes = 60;
    public static readonly int DefaultCleanupIntervalMinutes = 60;

    public static readonly string WorkerSettingsSectionName = "WorkerSettings";
    public static readonly int DefaultAnalysisConcurrentWorkers = 5;
    public static readonly int DefaultAnalysisTimeoutMinutes = 10;

    public static readonly string AnalysisStatusFileName = "status.json";
    public static readonly string AnalysisMetadataFileName = "metadata.json";
    public static readonly string AnalysisStatisticsFileName = "statistics.json";
    public static readonly string DecompiledZipFileName = "decompiled_source.zip";
    public static readonly string AnalysisNamespaceStructureFileName = "namespace-tree.json";
}

/*public static class ProjectConstants_AssemblyStorage
{
    public static readonly string SectionName = "StorageOptions";
    public static readonly string DefaultStoragePath = "AnalysisFiles";
    public static readonly int DefaultFileLifetimeMinutes = 60;
    public static readonly int DefaultCleanupIntervalMinutes = 60;
}

public static class ProjectConstants_WorkerSettings
{
    public static readonly string SettingsSectionName = "WorkerSettings";
    public static readonly int DefaultAssemblyAnalysisConcurrentWorkers = 5;
    public static readonly int DefaultAssemblyAnalysisTimeoutMinutes = 10;
    public static readonly int DefaultAssemblyDecompilationConcurrentWorkers = 5;
    public static readonly int DefaultAssemblyDecompilationTimeoutMinutes = 10;
}*/
