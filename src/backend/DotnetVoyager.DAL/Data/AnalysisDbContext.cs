using DotnetVoyager.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DotnetVoyager.DAL.Data;

public class AnalysisDbContext : DbContext
{
    public DbSet<AnalysisStatus> AnalysisStatuses { get; set; }

    public AnalysisDbContext(DbContextOptions<AnalysisDbContext> options)
        : base(options)
    {
    }
}
