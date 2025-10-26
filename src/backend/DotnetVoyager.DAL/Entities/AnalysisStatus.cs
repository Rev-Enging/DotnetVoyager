using DotnetVoyager.BLL.Enums;
using System.ComponentModel.DataAnnotations;

namespace DotnetVoyager.DAL.Entities;

public class AnalysisStatus
{
    [Key]
    public string AnalysisId { get; set; } = null!;

    // Main analysis status
    public AssemblyAnalysisStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    // Zip generation status
    public ZipGenerationStatus ZipStatus { get; set; }
    public string? ZipErrorMessage { get; set; }

    // Other useful data
    public string? OriginalFileName { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastUpdatedUtc { get; set; }
}
