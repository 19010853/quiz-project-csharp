using Identity.API.Database;
using Identity.API.Models;
using Identity.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
var migrationsAssembly = typeof(Program).Assembly.GetName().Name;

void ConfigureSqlServer(DbContextOptionsBuilder options) =>
    options.UseSqlServer(connectionString, sql =>
    {
        sql.MigrationsAssembly(migrationsAssembly);
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(ConfigureSqlServer);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
    {
        options.Authentication.CookieLifetime = TimeSpan.FromHours(2);
    })
    .AddAspNetIdentity<ApplicationUser>()
    .AddConfigurationStore(options => options.ConfigureDbContext = ConfigureSqlServer)
    .AddOperationalStore(options => options.ConfigureDbContext = ConfigureSqlServer)
    .AddProfileService<ProfileService>();

var app = builder.Build();

// Run "dotnet run -- /seed" to apply migrations and seed IdentityServer configuration
if (args.Contains("/seed"))
{
    await SeedData.EnsureSeedDataAsync(app);
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseIdentityServer();

app.UseAuthorization();

app.Run();
