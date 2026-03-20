using ClearMeasure.Bootcamp.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class EmployeeMap : IEntityFrameworkMapping
{
    public void Map(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee", "dbo");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .IsRequired()
                .ValueGeneratedNever();
            entity.Property(e => e.UserName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(200);
        });
    }
}
