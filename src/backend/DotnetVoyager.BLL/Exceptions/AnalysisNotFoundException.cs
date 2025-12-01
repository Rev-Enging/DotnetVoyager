namespace DotnetVoyager.BLL.Exceptions;

public sealed class AnalysisNotFoundException : Exception
{
    public string AnalysisId { get; init; }

    public AnalysisNotFoundException(string analysisId)
        : base($"The analysis with ID '{analysisId}' was not found.")
    {
        AnalysisId = analysisId;
    }

    public static void ThrowIfAnalysisNotFound(bool analysisExists, string analysisId)
    {
        if (!analysisExists)
        {
            throw new AnalysisNotFoundException(analysisId);
        }
    }
}
