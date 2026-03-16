using Microsoft.EntityFrameworkCore;
using OutboxPatterns.Domain;

namespace OutboxPatterns.Infrastructure;

public sealed class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<Userss> Users => Set<Userss>();
    public DbSet<OutboxTable> OutboxTables => Set<OutboxTable>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Userss>(entity =>
        {
            entity.HasKey(x=>x.Id);
            entity.Property<Guid>("UserId")
            .IsRequired();
            entity.Property<string>("Name")
            .HasMaxLength(40)
            .IsRequired();
            entity.Property<string>("Email")
            .HasMaxLength(60)
            .IsRequired();
            entity.Property<string>("Password")
            .HasMaxLength(100)
            .IsRequired();

        });

        modelBuilder.Entity<OutboxTable>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType)
            .HasMaxLength(200)
            .IsRequired();
            entity.Property(x => x.Payload)
            .IsRequired();
            entity.Property(x => x.OccurredOn)
            .IsRequired();
            entity.Property(x => x.ProcessedOn);
            entity.Property(x => x.Error)
            .HasMaxLength(500);
        });

    }
}
