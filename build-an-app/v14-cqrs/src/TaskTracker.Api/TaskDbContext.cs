using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Api;

public sealed class TaskEntity
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsDone { get; set; }
}

public sealed class TaskDbContext : DbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(task => task.Id);
            entity.Property(task => task.Title).IsRequired().HasMaxLength(200);
        });
    }
}

