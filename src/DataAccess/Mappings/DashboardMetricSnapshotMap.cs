using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Entity Framework Core mapping for DashboardMetricSnapshot
/// </summary>
public class DashboardMetricSnapshotMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DashboardMetricSnapshotEntity>(entity =>
        {
            entity.ToTable("DashboardMetricSnapshot", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.MetricName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Value)
                .IsRequired()
                .HasColumnType("DECIMAL(18,4)");
            entity.Property(e => e.PeriodStart).IsRequired();
            entity.Property(e => e.PeriodEnd).IsRequired();
            entity.Property(e => e.ComputedAt).IsRequired();

            entity.HasIndex(e => new { e.PeriodStart, e.PeriodEnd });
            entity.HasIndex(e => e.MetricName);
        });
    }
}

/// <summary>
/// Persistence entity for dashboard metric snapshots
/// </summary>
public class DashboardMetricSnapshotEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MetricName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public DateTimeOffset ComputedAt { get; set; }
}
