using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;
using EMR.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Data.Common;
using System.Linq.Expressions;

namespace EMR.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory for integration tests
/// Configures SQLite in-memory database and test-specific services
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection;
    private bool _databaseInitialized;

    public CustomWebApplicationFactory()
    {
        // Set environment to Testing BEFORE the host is built
        // This ensures appsettings.Testing.json is loaded by Program.cs
        // AND tells Infrastructure.DependencyInjection to skip Npgsql registration
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        // Create and open a shared SQLite connection for all tests
        // Using a shared connection ensures database persistence during tests
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration to disable Azure Key Vault for tests (belt and suspenders)
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "KeyVault:Url", "" }, // Disable KeyVault for tests
                { "ConnectionStrings:DefaultConnection", "" }, // SQLite will be configured
                { "ConnectionStrings:Redis", "" } // Disable Redis for tests
            });
        });

        builder.ConfigureServices(services =>
        {
            // Infrastructure skips DbContext registration in Testing environment
            // So we add SQLite in-memory database here

            // Add SQLite in-memory database for ApplicationDbContext (write)
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Add SQLite in-memory database for ReadDbContext (read)
            services.AddDbContext<ReadDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Forcefully add MemoryCache - remove existing and add fresh
            services.RemoveAll<IMemoryCache>();
            services.RemoveAll<MemoryCache>();
            services.AddMemoryCache();

            // Configure antiforgery for testing (disable SSL requirement)
            services.PostConfigure<Microsoft.AspNetCore.Antiforgery.AntiforgeryOptions>(options =>
            {
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
            });

            // Forcefully replace read model repositories with test implementations
            services.RemoveAll<IPatientReadModelRepository>();
            services.RemoveAll<IPatientDetailReadModelRepository>();
            services.AddScoped<IPatientReadModelRepository, TestPatientReadModelRepository>();
            services.AddScoped<IPatientDetailReadModelRepository, TestPatientDetailReadModelRepository>();
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Creates an HTTP client and ensures the database is initialized
    /// </summary>
    public new HttpClient CreateClient()
    {
        EnsureDatabaseInitialized();
        return base.CreateClient();
    }

    /// <summary>
    /// Ensures the database schema is created (called automatically when creating client)
    /// </summary>
    private void EnsureDatabaseInitialized()
    {
        if (_databaseInitialized)
            return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        _databaseInitialized = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
        }
    }
}

/// <summary>
/// Test implementation of IPatientReadModelRepository for integration tests
/// </summary>
internal class TestPatientReadModelRepository : IPatientReadModelRepository
{
    private readonly List<PatientSummaryReadModel> _store = new();

    // IReadModelRepository<PatientSummaryReadModel> base methods
    public Task<PatientSummaryReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<PatientSummaryReadModel>> GetAsync(
        Expression<Func<PatientSummaryReadModel, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = predicate != null ? _store.AsQueryable().Where(predicate) : _store.AsQueryable();
        return Task.FromResult<IReadOnlyList<PatientSummaryReadModel>>(query.ToList());
    }

    public Task<(IReadOnlyList<PatientSummaryReadModel> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<PatientSummaryReadModel, bool>>? predicate = null,
        Expression<Func<PatientSummaryReadModel, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = predicate != null ? _store.AsQueryable().Where(predicate) : _store.AsQueryable();
        var total = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<PatientSummaryReadModel>, int)>((items, total));
    }

    public Task<bool> AnyAsync(Expression<Func<PatientSummaryReadModel, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.AsQueryable().Any(predicate));

    public Task<int> CountAsync(Expression<Func<PatientSummaryReadModel, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate != null ? _store.AsQueryable().Count(predicate) : _store.Count);

    public Task UpsertAsync(PatientSummaryReadModel readModel, CancellationToken cancellationToken = default)
    {
        var existing = _store.FirstOrDefault(x => x.Id == readModel.Id);
        if (existing != null) _store.Remove(existing);
        _store.Add(readModel);
        return Task.CompletedTask;
    }

    public Task UpsertRangeAsync(IEnumerable<PatientSummaryReadModel> readModels, CancellationToken cancellationToken = default)
    {
        foreach (var model in readModels)
        {
            var existing = _store.FirstOrDefault(x => x.Id == model.Id);
            if (existing != null) _store.Remove(existing);
            _store.Add(model);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        _store.RemoveAll(x => idSet.Contains(x.Id));
        return Task.CompletedTask;
    }

    public Task DeleteWhereAsync(Expression<Func<PatientSummaryReadModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var toRemove = _store.AsQueryable().Where(predicate).ToList();
        foreach (var item in toRemove) _store.Remove(item);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    // IPatientReadModelRepository specific methods
    public Task<IReadOnlyList<PatientSummaryReadModel>> SearchAsync(string searchText, int maxResults = 50, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PatientSummaryReadModel>>(
            _store.Where(x => x.SearchText.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                  .Take(maxResults).ToList());

    public Task<IReadOnlyList<PatientSummaryReadModel>> GetByProviderAsync(Guid providerId, bool activeOnly = true, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PatientSummaryReadModel>>(
            _store.Where(x => x.PrimaryCareProviderId == providerId && (!activeOnly || x.Status == "Active")).ToList());

    public Task<IReadOnlyList<PatientSummaryReadModel>> GetWithActiveAlertsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PatientSummaryReadModel>>(_store.Where(x => x.ActiveAlertsCount > 0).ToList());

    public Task<IReadOnlyList<PatientSummaryReadModel>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PatientSummaryReadModel>>(_store.Where(x => x.Status == status).ToList());

    public Task<PatientSummaryReadModel?> GetByMRNAsync(string mrn, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(x => x.MRN == mrn));
}

/// <summary>
/// Test implementation of IPatientDetailReadModelRepository for integration tests
/// </summary>
internal class TestPatientDetailReadModelRepository : IPatientDetailReadModelRepository
{
    private readonly List<PatientDetailReadModel> _store = new();

    // IReadModelRepository<PatientDetailReadModel> base methods
    public Task<PatientDetailReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<PatientDetailReadModel>> GetAsync(
        Expression<Func<PatientDetailReadModel, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = predicate != null ? _store.AsQueryable().Where(predicate) : _store.AsQueryable();
        return Task.FromResult<IReadOnlyList<PatientDetailReadModel>>(query.ToList());
    }

    public Task<(IReadOnlyList<PatientDetailReadModel> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<PatientDetailReadModel, bool>>? predicate = null,
        Expression<Func<PatientDetailReadModel, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = predicate != null ? _store.AsQueryable().Where(predicate) : _store.AsQueryable();
        var total = query.Count();
        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult<(IReadOnlyList<PatientDetailReadModel>, int)>((items, total));
    }

    public Task<bool> AnyAsync(Expression<Func<PatientDetailReadModel, bool>> predicate, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.AsQueryable().Any(predicate));

    public Task<int> CountAsync(Expression<Func<PatientDetailReadModel, bool>>? predicate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(predicate != null ? _store.AsQueryable().Count(predicate) : _store.Count);

    public Task UpsertAsync(PatientDetailReadModel readModel, CancellationToken cancellationToken = default)
    {
        var existing = _store.FirstOrDefault(x => x.Id == readModel.Id);
        if (existing != null) _store.Remove(existing);
        _store.Add(readModel);
        return Task.CompletedTask;
    }

    public Task UpsertRangeAsync(IEnumerable<PatientDetailReadModel> readModels, CancellationToken cancellationToken = default)
    {
        foreach (var model in readModels)
        {
            var existing = _store.FirstOrDefault(x => x.Id == model.Id);
            if (existing != null) _store.Remove(existing);
            _store.Add(model);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        _store.RemoveAll(x => idSet.Contains(x.Id));
        return Task.CompletedTask;
    }

    public Task DeleteWhereAsync(Expression<Func<PatientDetailReadModel, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var toRemove = _store.AsQueryable().Where(predicate).ToList();
        foreach (var item in toRemove) _store.Remove(item);
        return Task.CompletedTask;
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    // IPatientDetailReadModelRepository specific methods
    public Task<PatientDetailReadModel?> GetByMRNAsync(string mrn, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(x => x.MRN == mrn));

    public Task<IReadOnlyList<PatientDetailReadModel>> GetWithActiveMedicationsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PatientDetailReadModel>>(_store.Where(x => x.ActiveMedications.Any()).ToList());

    public Task<IReadOnlyList<PatientDetailReadModel>> GetWithAllergyAsync(string allergen, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PatientDetailReadModel>>(
            _store.Where(x => x.ActiveAllergies.Any(a => a.Allergen.Contains(allergen, StringComparison.OrdinalIgnoreCase))).ToList());
}
