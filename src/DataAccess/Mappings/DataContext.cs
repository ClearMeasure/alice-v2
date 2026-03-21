using ClearMeasure.Bootcamp.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Mappings;

public class DataContext : DbContext
{
    private readonly IDatabaseConfiguration _config;

    public DataContext(IDatabaseConfiguration config, ILogger<DataContext>? logger = null)
    {
        _config = config;
        logger?.LogDebug(ToString());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();

        optionsBuilder.UseSqlServer(_config.GetConnectionString());

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new EmployeeMap().Map(modelBuilder);
    }

    public sealed override string ToString()
    {
        return base.ToString() + "-" + GetHashCode();
    }
}
