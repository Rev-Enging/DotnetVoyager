namespace DotnetVoyager.BLL.Errors;

public sealed class AnalysisWithIdNotFound : NotFoundError
{
    public string AnalysisId { get; }

    public AnalysisWithIdNotFound(string analysisId)
        : base($"Analysis with ID '{analysisId}' not found.") 
    {
        if (string.IsNullOrWhiteSpace(analysisId))
        {
            throw new ArgumentException("Analysis ID cannot be null or whitespace.", nameof(analysisId));
        }

        AnalysisId = analysisId;
    }
}
