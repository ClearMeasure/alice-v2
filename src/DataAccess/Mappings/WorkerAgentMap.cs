using ClearMeasure.Bootcamp.DataAccess.Handlers.Factory;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

/// <summary>
/// Entity Framework Core mapping for WorkerAgentRegistration and ExecutionLog
/// </summary>
public class WorkerAgentMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkerAgentRegistrationEntity>(entity =>
        {
            entity.ToTable("WorkerAgentRegistration", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.AgentName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.TargetStatusCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.AgentType)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Configuration);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
        });

        modelBuilder.Entity<WorkerAgentExecutionLogEntity>(entity =>
        {
            entity.ToTable("WorkerAgentExecutionLog", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.AgentName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.FactoryWorkItemId).IsRequired();
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.CompletedAt);
            entity.Property(e => e.Success);
            entity.Property(e => e.Summary);
            entity.Property(e => e.OutputData);
        });
    }
}

/// <summary>
/// Persistence entity for worker agent registrations
/// </summary>
public class WorkerAgentRegistrationEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AgentName { get; set; } = string.Empty;
    public string TargetStatusCode { get; set; } = string.Empty;
    public string AgentType { get; set; } = "InProcess";
    public string? Configuration { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedDate { get; set; }
}
