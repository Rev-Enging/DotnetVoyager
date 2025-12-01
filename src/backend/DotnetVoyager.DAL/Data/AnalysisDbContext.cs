using DotnetVoyager.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetVoyager.DAL.Data;

public class AnalysisDbContext : DbContext
{
    public DbSet<AnalysisStatus> AnalysisStatuses { get; set; }
    public DbSet<AnalysisStep> AnalysisSteps { get; set; }

    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure relationship with cascade delete
        modelBuilder.Entity<AnalysisStatus>()
            .HasMany(a => a.Steps)
            .WithOne(s => s.Analysis)
            .HasForeignKey(s => s.AnalysisId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure one step per name per analysis
        modelBuilder.Entity<AnalysisStep>()
            .HasIndex(s => new { s.AnalysisId, s.StepName })
            .IsUnique();

        // Index for querying pending steps
        modelBuilder.Entity<AnalysisStep>()
            .HasIndex(s => new { s.Status, s.AnalysisId });

        // Index for cleanup queries (by LastUpdatedUtc)
        modelBuilder.Entity<AnalysisStatus>()
            .HasIndex(a => a.LastUpdatedUtc);
    }
}