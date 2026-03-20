using ClearMeasure.Bootcamp.Core.Model.Factory;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Entity Framework Core mapping for FactoryWorkItem, StatusTransition, and FactoryEvent
/// </summary>
public class FactoryWorkItemMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FactoryWorkItem>(entity =>
        {
            entity.ToTable("FactoryWorkItem", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.ExternalId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.ExternalSystem)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.WorkItemType)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("WorkItemTypeCode")
                .HasConversion(
                    v => v.Code,
                    v => WorkItemType.FromCode(v));
            entity.Property(e => e.CurrentStatus)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("CurrentStatusCode")
                .HasConversion(
                    v => v.Code,
                    v => FactoryStatus.FromCode(v));
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.LastStatusChangeDate).IsRequired();

            entity.HasIndex(e => new { e.ExternalId, e.ExternalSystem }).IsUnique();

            entity.Ignore(e => e.StatusHistory);
        });

        modelBuilder.Entity<StatusTransition>(entity =>
        {
            entity.ToTable("StatusTransition", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.FactoryWorkItemId).IsRequired();
            entity.Property(e => e.FromStatus)
                .HasMaxLength(50)
                .HasColumnName("FromStatusCode")
                .HasConversion(
                    v => v == null ? null : v.Code,
                    v => v == null ? null : FactoryStatus.FromCode(v));
            entity.Property(e => e.ToStatus)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("ToStatusCode")
                .HasConversion(
                    v => v.Code,
                    v => FactoryStatus.FromCode(v));
            entity.Property(e => e.TransitionDate).IsRequired();
            entity.Property<bool>("IsBackward").HasColumnName("IsBackward");
            entity.Ignore(e => e.IsBackward);

            entity.HasIndex(e => e.FactoryWorkItemId);
        });

        modelBuilder.Entity<FactoryEventEntity>(entity =>
        {
            entity.ToTable("FactoryEvent", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.EventTypeCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.FactoryWorkItemId);
            entity.Property(e => e.ExternalId)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.ExternalSystem)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Payload);
            entity.Property(e => e.OccurredAt).IsRequired();

            entity.HasIndex(e => e.FactoryWorkItemId);
            entity.HasIndex(e => e.OccurredAt);
        });
    }
}

/// <summary>
/// Persistence entity for factory events (separate from the MediatR notification)
/// </summary>
public class FactoryEventEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventTypeCode { get; set; } = string.Empty;
    public Guid? FactoryWorkItemId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string ExternalSystem { get; set; } = string.Empty;
    public string? Payload { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
