using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EMR.Infrastructure.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext to support EF Core migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Use a default connection string for migrations
        // This will be overridden at runtime by the actual configuration
        var connectionString = "Host=localhost;Port=5432;Database=emr_db;Username=postgres;Password=postgres";

        // Create DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
