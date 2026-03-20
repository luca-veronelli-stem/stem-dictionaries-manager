using Infrastructure.Entities;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<BoardTypeEntity> BoardTypes => Set<BoardTypeEntity>();
    public DbSet<BoardEntity> Boards => Set<BoardEntity>();
    public DbSet<VariableEntity> Variables => Set<VariableEntity>();
    public DbSet<DictionaryEntity> Dictionaries => Set<DictionaryEntity>();
    public DbSet<BitInterpretationEntity> BitInterpretations => Set<BitInterpretationEntity>();
    public DbSet<CommandEntity> Commands => Set<CommandEntity>();
    public DbSet<CommandDeviceStateEntity> CommandDeviceStates => Set<CommandDeviceStateEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    public override int SaveChanges()
    {
        SetAuditFields();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetAuditFields();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetAuditFields();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SetAuditFields()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
        });

        // BoardType
        modelBuilder.Entity<BoardTypeEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
        });

        // Board
        modelBuilder.Entity<BoardEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProtocolAddress).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PartNumber).HasMaxLength(20);
            entity.HasOne(e => e.BoardType)
                  .WithMany(bt => bt.Boards)
                  .HasForeignKey(e => e.BoardTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Dictionary
        modelBuilder.Entity<DictionaryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => new { e.DeviceType, e.BoardTypeId }).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.BoardType)
                  .WithMany(bt => bt.Dictionaries)
                  .HasForeignKey(e => e.BoardTypeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Variable
        modelBuilder.Entity<VariableEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DictionaryId, e.AddressHigh, e.AddressLow }).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DataTypeRaw).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Format).HasMaxLength(50);
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.Usage).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasOne(e => e.Dictionary)
                  .WithMany(d => d.Variables)
                  .HasForeignKey(e => e.DictionaryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // BitInterpretation
        modelBuilder.Entity<BitInterpretationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VariableId, e.WordIndex, e.BitIndex }).IsUnique();
            entity.Property(e => e.Meaning).HasMaxLength(200);
            entity.HasOne(e => e.Variable)
                  .WithMany(v => v.BitInterpretations)
                  .HasForeignKey(e => e.VariableId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Command
        modelBuilder.Entity<CommandEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CodeHigh, e.CodeLow, e.IsResponse }).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ParametersJson).HasMaxLength(1000);
        });

        // CommandDeviceState
        modelBuilder.Entity<CommandDeviceStateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CommandId, e.DeviceType }).IsUnique();
            entity.HasOne(e => e.Command)
                  .WithMany(c => c.DeviceStates)
                  .HasForeignKey(e => e.CommandId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditEntry
        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.ChangedAt);
            entity.Property(e => e.PreviousValue).HasColumnType("TEXT");
            entity.Property(e => e.NewValue).HasColumnType("TEXT");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.ChangedBy)
                  .WithMany(u => u.AuditEntries)
                  .HasForeignKey(e => e.ChangedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
