using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class WorkItemEventMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkItemEvent>(entity =>
        {
            entity.ToTable("WorkItemEvent", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.WorkItemExternalId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.Source)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.PreviousStatus)
                .HasMaxLength(200);
            entity.Property(e => e.NewStatus)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.OccurredAtUtc)
                .IsRequired();
            entity.Property(e => e.ReceivedAtUtc)
                .IsRequired();
            entity.Property(e => e.RawPayload)
                .IsRequired();

            entity.HasIndex(e => new { e.WorkItemExternalId, e.Source });
        });
    }
}
