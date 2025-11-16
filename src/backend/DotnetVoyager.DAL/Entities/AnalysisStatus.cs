using DotnetVoyager.DAL.Enums;
using System.ComponentModel.DataAnnotations;

namespace DotnetVoyager.DAL.Entities;

public class AnalysisStatus
{
    [Key]
    public string AnalysisId { get; set; } = null!;

    public AnalysisOverallStatus OverallStatus { get; set; }

    public string? OriginalFileName { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public ICollection<AnalysisStep> Steps { get; set; } = new List<AnalysisStep>();
}