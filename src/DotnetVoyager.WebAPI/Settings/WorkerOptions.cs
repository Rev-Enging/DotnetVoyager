using DotnetVoyager.WebAPI.Constants;

namespace DotnetVoyager.WebAPI.Settings;

public class WorkerOptions
{
    public int AnalysisConcurrentWorkers { get; init; } = ProjectConstants.DefaultAnalysisConcurrentWorkers;
    public int AnalysisTimeoutMinutes { get; init; } = ProjectConstants.DefaultAnalysisTimeoutMinutes;
}
