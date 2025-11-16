using DotnetVoyager.DAL.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DotnetVoyager.DAL.Entities;

public class AnalysisStep
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string AnalysisId { get; set; } = null!;

    // e.g., "Metadata", "Statistics"
    [Required, MaxLength(100)]
    public string StepName { get; set; } = null!;

    public AnalysisStepStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }

    public int RetryCount { get; set; } = 0;

    [ForeignKey(nameof(AnalysisId))]
    public AnalysisStatus Analysis { get; set; } = null!;
}