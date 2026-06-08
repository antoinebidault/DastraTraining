using Microsoft.EntityFrameworkCore;

public class WorkItemDbContext : DbContext
{
    public WorkItemDbContext(DbContextOptions<WorkItemDbContext> options) : base(options)
    {
    }

    public DbSet<WorkItem> WorkItems { get; set; }
}