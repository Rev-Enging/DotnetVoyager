namespace DotnetVoyager.BLL.Constants;

public static class ProjectConstants
{
    // TODO: Make separarate classes like ProjectConstants_WorkerSettings and ProjectConstants_AssemblyStorage

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
    public static readonly string AnalysisInheritanceGraphFileName = "inheritance-graph.json";
    public static readonly string AnalysisNamespaceStructureFileName = "namespace-tree.json";
}
