namespace DotnetVoyager.BLL.Errors;

public sealed class AnalysisNotFound : NotFoundError
{
    public string AnalysisId { get; }

    public AnalysisNotFound(string analysisId)
        : base($"Analysis with ID '{analysisId}' not found.") 
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            throw new ArgumentException("Analysis ID cannot be null or whitespace.", nameof(analysisId));
        }

        AnalysisId = analysisId;
    }
}
