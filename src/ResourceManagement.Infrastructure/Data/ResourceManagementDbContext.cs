using Microsoft.EntityFrameworkCore;
using ResourceManagement.Core.Entities;

namespace ResourceManagement.Infrastructure.Data;

public class ResourceManagementDbContext : DbContext
{
    public ResourceManagementDbContext(DbContextOptions<ResourceManagementDbContext> options)
        : base(options) { }

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ForecastAllocation> ForecastAllocations => Set<ForecastAllocation>();
    public DbSet<IlcClaim> IlcClaims => Set<IlcClaim>();
    public DbSet<IlcUploadBatch> IlcUploadBatches => Set<IlcUploadBatch>();
    public DbSet<LeaveRecord> LeaveRecords => Set<LeaveRecord>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectAllocation> ProjectAllocations => Set<ProjectAllocation>();
    public DbSet<SkillMatrix> SkillMatrices => Set<SkillMatrix>();
    public DbSet<ResourceMovement> ResourceMovements => Set<ResourceMovement>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<BandMixRecord> BandMixRecords => Set<BandMixRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Resource
        modelBuilder.Entity<Resource>(e =>
        {
            e.HasIndex(r => r.EmpId).IsUnique();
            e.HasIndex(r => r.TalentId).IsUnique();
            e.HasIndex(r => r.IntranetId);
            e.Property(r => r.CostRate).HasColumnType("decimal(18,4)");
        });

        // ForecastAllocation
        modelBuilder.Entity<ForecastAllocation>(e =>
        {
            e.HasIndex(fa => new { fa.ResourceId, fa.Year, fa.Month }).IsUnique();
            e.Property(fa => fa.ForecastHours).HasColumnType("decimal(18,2)");
            e.Property(fa => fa.ForecastCost).HasColumnType("decimal(18,2)");
            e.Property(fa => fa.FteFraction).HasColumnType("decimal(5,4)");
            e.Property(fa => fa.ActualHours).HasColumnType("decimal(18,2)");
        });

        // IlcClaim
        modelBuilder.Entity<IlcClaim>(e =>
        {
            e.HasIndex(c => new { c.ResourceId, c.WeekEndingDate });
            e.Property(c => c.ClaimedHours).HasColumnType("decimal(18,2)");
        });

        // ProjectAllocation
        modelBuilder.Entity<ProjectAllocation>(e =>
        {
            e.HasIndex(pa => new { pa.ResourceId, pa.ProjectId });
            e.Property(pa => pa.WeeklyHours).HasColumnType("decimal(18,2)");
            e.Property(pa => pa.BudgetedHours).HasColumnType("decimal(18,2)");
            e.Property(pa => pa.ConsumedHours).HasColumnType("decimal(18,2)");
            e.Property(pa => pa.FteFraction).HasColumnType("decimal(5,4)");
        });

        // Project
        modelBuilder.Entity<Project>(e =>
        {
            e.HasIndex(p => p.ProjectCode).IsUnique();
            e.Property(p => p.TotalBudgetHours).HasColumnType("decimal(18,2)");
            e.Property(p => p.ConsumedHours).HasColumnType("decimal(18,2)");
        });

        // Holiday
        modelBuilder.Entity<Holiday>(e =>
        {
            e.HasIndex(h => new { h.Year, h.Date, h.Location }).IsUnique();
        });

        // BandMixRecord
        modelBuilder.Entity<BandMixRecord>(e =>
        {
            e.HasIndex(b => new { b.Year, b.Month, b.Band });
            e.Property(b => b.Weightage).HasColumnType("decimal(5,2)");
            e.Property(b => b.TotalBandValue).HasColumnType("decimal(18,4)");
            e.Property(b => b.BandPercentage).HasColumnType("decimal(10,4)");
            e.Property(b => b.BandMix).HasColumnType("decimal(10,4)");
        });

        // LeaveRecord
        modelBuilder.Entity<LeaveRecord>(e =>
        {
            e.Property(l => l.ForecastImpactHours).HasColumnType("decimal(18,2)");
        });
    }
}
