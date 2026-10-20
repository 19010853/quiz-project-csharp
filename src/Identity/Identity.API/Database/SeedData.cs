using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Database;

public static class SeedData
{
    // Applies pending migrations for all Identity.API databases and seeds the
    // IdentityServer configuration (clients, resources, scopes) from Config.cs.
    public static async Task EnsureSeedDataAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SeedData));

        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();

        var configurationDb = services.GetRequiredService<ConfigurationDbContext>();
        await configurationDb.Database.MigrateAsync();

        if (!await configurationDb.Clients.AnyAsync())
        {
            configurationDb.Clients.AddRange(Config.Clients.Select(c => c.ToEntity()));
            logger.LogInformation("Seeded {Count} clients", Config.Clients.Count());
        }

        if (!await configurationDb.IdentityResources.AnyAsync())
        {
            configurationDb.IdentityResources.AddRange(Config.IdentityResources.Select(r => r.ToEntity()));
            logger.LogInformation("Seeded {Count} identity resources", Config.IdentityResources.Count());
        }

        if (!await configurationDb.ApiScopes.AnyAsync())
        {
            configurationDb.ApiScopes.AddRange(Config.ApiScopes.Select(s => s.ToEntity()));
            logger.LogInformation("Seeded {Count} API scopes", Config.ApiScopes.Count());
        }

        if (!await configurationDb.ApiResources.AnyAsync())
        {
            configurationDb.ApiResources.AddRange(Config.ApiResources.Select(r => r.ToEntity()));
            logger.LogInformation("Seeded {Count} API resources", Config.ApiResources.Count());
        }

        await configurationDb.SaveChangesAsync();
    }
}
