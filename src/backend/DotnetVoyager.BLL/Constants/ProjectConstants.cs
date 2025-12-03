using DotnetVoyager.BLL.Enums;

namespace DotnetVoyager.BLL.Constants;

public static class ProjectConstants
{
    public const int MaxAssemblySizeInMegabytes = 3;
    public const int MaxAssemblySizeInBytes = MaxAssemblySizeInMegabytes * 1024 * 1024;

    public const string CorsOptionsSectionName = "CorsOptions";

    public const string AssemblyStorageSettingsSectionName = "StorageOptions";
    public const string DefaultAnalysisStoragePath = "AnalysisFiles";
    public const int DefaultFileLifetimeMinutes = 60;
    public const int DefaultCleanupIntervalMinutes = 60;

    public const string WorkerSettingsSectionName = "WorkerSettings";
    public const int DefaultAnalysisConcurrentWorkers = 5;
    public const int DefaultAnalysisTimeoutMinutes = 10;

    public const string AnalysisMetadataFileName = "metadata.json";
    public const string AnalysisStatisticsFileName = "statistics.json";
    public const string AnalysisInheritanceGraphFileName = "inheritance-graph.json";
    public const string AssemblyTreeFileName = "assembly-tree.json";
    public const string AssemblyDependenciesFileName = "assembly-dependencies.json";
    public const string DecompiledZipFileName = "decompiled_source.zip";
}

public static class AnalysisStepNames
{
    public const string Metadata = "Metadata";
    public const string Statistics = "Statistics";
    public const string AssemblyTree = "AssemblyTree";
    public const string AssemblyDependencies = "AssemblyDependencies";
    public const string InheritanceGraph = "InheritanceGraph";
    public const string ZipGeneration = "ZipGeneration";

    public static readonly IReadOnlyCollection<string> AllSteps =
    [
        Metadata,
        Statistics,
        AssemblyTree,
        AssemblyDependencies,
        InheritanceGraph,
        ZipGeneration
    ];

    public static readonly IReadOnlyCollection<string> RequiredSteps =
    [
        Metadata,
        Statistics,
        AssemblyTree,
        AssemblyDependencies,
        InheritanceGraph
    ];

    public static bool IsValidStep(string stepName)
        => AllSteps.Contains(stepName);

    public static bool IsRequired(string stepName)
        => RequiredSteps.Contains(stepName);

    public static bool IsOptional(string stepName)
        => !IsRequired(stepName);

    public static IEnumerable<string> GetRequiredSteps()
        => RequiredSteps;

    public static string ToStepName(this AnalysisStepName stepName)
    => stepName.ToString();

    public static AnalysisStepName? ToStepEnum(string stepName)
        => Enum.TryParse<AnalysisStepName>(stepName, out var result) ? result : null;
}