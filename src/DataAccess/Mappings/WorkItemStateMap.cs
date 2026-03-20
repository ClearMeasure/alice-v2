using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkItemStateMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkItemState>(entity =>
        {
            entity.ToTable("WorkItemState", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.ExternalId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.CurrentStatus)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.ProjectName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.LastUpdatedAtUtc)
                .IsRequired();

            entity.HasIndex(e => new { e.ExternalId, e.Source })
                .IsUnique();
        });
    }
}
