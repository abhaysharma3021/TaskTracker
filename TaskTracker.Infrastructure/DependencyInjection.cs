using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskTracker.Domain.Interfaces;
using TaskTracker.Infrastructure.Data;
using TaskTracker.Infrastructure.Repositories;

namespace TaskTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // Retrieve the SQLite connection string from configuration
        string connectionString = configuration.GetConnectionString("DefaultConnection")
                                  ?? throw new Exception("Connection string not found in appsettings.json");

        // ✅ Extract ONLY the database path from the SQLite connection string
        string databaseFilePath = ExtractDatabaseFilePath(connectionString);
        string databaseFolder = Path.GetDirectoryName(databaseFilePath) ?? "";

        //// 🔍 Debugging: Print extracted paths
        //Console.WriteLine($"🔍 Extracted DB File Path: {databaseFilePath}");
        //Console.WriteLine($"🔍 Extracted DB Folder: {databaseFolder}");

        // ✅ Ensure the database folder exists
        if (!string.IsNullOrEmpty(databaseFolder) && !Directory.Exists(databaseFolder))
        {
            Console.WriteLine($"✅ Creating directory: {databaseFolder}");
            Directory.CreateDirectory(databaseFolder);
        }

        // Add the SQLite DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite(
                connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
            );
        });

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }

    // ✅ Helper function to correctly extract the SQLite database file path
    private static string ExtractDatabaseFilePath(string connectionString)
    {
        var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"Data Source=(.+);?");
        if (match.Success)
        {
            string dbFilePath = match.Groups[1].Value.Trim();
            return dbFilePath;
        }
        throw new Exception("❌ Invalid SQLite connection string format.");
    }
}
