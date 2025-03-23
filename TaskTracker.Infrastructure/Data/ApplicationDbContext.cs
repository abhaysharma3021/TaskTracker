using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Task> Tasks { get; set; }
}
