using Microsoft.EntityFrameworkCore;
using Sofra.Worker.Entities;

namespace Sofra.Worker.Data;

/// <summary>
/// Worker je zaseban proces bez pristupa Sofra.API entitetima/migracijama - ovaj DbContext mapira
/// samo tabele koje Worker stvarno pise (Notifications, ProcessedEvents), na istu bazu koju API
/// kreira svojim migracijama. Worker nikad ne generise svoje migracije.
/// </summary>
public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("Notifications");
            builder.Property(x => x.Title).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Text).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<ProcessedEvent>(builder =>
        {
            builder.ToTable("ProcessedEvents");
            builder.HasKey(x => x.EventId);
        });
    }
}
