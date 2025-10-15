using DotnetVoyager.BLL.Constants;

namespace DotnetVoyager.BLL.Options;

public class WorkerOptions
{
    public int AnalysisConcurrentWorkers { get; init; } = ProjectConstants.DefaultAnalysisConcurrentWorkers;
    public int AnalysisTimeoutMinutes { get; init; } = ProjectConstants.DefaultAnalysisTimeoutMinutes;
}
