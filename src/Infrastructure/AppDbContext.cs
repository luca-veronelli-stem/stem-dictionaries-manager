using System.Text.Json;
using Infrastructure.Entities;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<BoardEntity> Boards => Set<BoardEntity>();
    public DbSet<VariableEntity> Variables => Set<VariableEntity>();
    public DbSet<DictionaryEntity> Dictionaries => Set<DictionaryEntity>();
    public DbSet<BitInterpretationEntity> BitInterpretations => Set<BitInterpretationEntity>();
    public DbSet<CommandEntity> Commands => Set<CommandEntity>();
    public DbSet<CommandDeviceStateEntity> CommandDeviceStates => Set<CommandDeviceStateEntity>();
    public DbSet<StandardVariableOverrideEntity> StandardVariableOverrides => Set<StandardVariableOverrideEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();

    // Bootstrap registration (spec 001)
    public DbSet<BootstrapTokenEntity> BootstrapTokens => Set<BootstrapTokenEntity>();
    public DbSet<InstallationEntity> Installations => Set<InstallationEntity>();
    public DbSet<InstallationApiCredentialEntity> InstallationApiCredentials => Set<InstallationApiCredentialEntity>();
    public DbSet<RegistrationEventEntity> RegistrationEvents => Set<RegistrationEventEntity>();

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
        DateTime now = DateTime.UtcNow;

        foreach (EntityEntry<IAuditable> entry in ChangeTracker.Entries<IAuditable>())
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

    /// <summary>
    /// Apply a UTC-pinning conversion to every <c>DateTime</c> / <c>DateTime?</c>
    /// property in the model. SQLite stores DateTime as TEXT without a Kind
    /// marker, so EF Core returns <c>DateTimeKind.Unspecified</c> on read;
    /// downstream JSON serialization then misses the <c>Z</c> suffix and
    /// parsers may treat the value as local time. SQL Server's
    /// <c>datetime2</c> has the same issue. Every domain DateTime in this
    /// repo is written as UTC (<see cref="DateTime.UtcNow"/> /
    /// <c>TimeProvider.GetUtcNow().UtcDateTime</c>), so re-stamping
    /// <see cref="DateTimeKind.Utc"/> on read is provider-agnostic and
    /// idempotent.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter()
            : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    private sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter()
            : base(
                v => v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null)
        {
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
            // Seed the system-admin user (data-model.md Audit split) into the
            // model so BOTH providers receive it: SQLite via EnsureCreated and
            // SQL Server via the existing AddBootstrapRegistration migration.
            // Id 1 matches the identity value that migration's InsertData
            // produced on a fresh SQL Server DB, so the Migrate path stays
            // consistent (the reconcile migration is a no-op).
            entity.HasData(new UserEntity
            {
                Id = 1,
                Username = "system-admin",
                DisplayName = "System Admin (API key)",
                CreatedAt = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // Device
        modelBuilder.Entity<DeviceEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.MachineCode).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            // BR-014: MachineCode must be > 0
            entity.ToTable(t => t.HasCheckConstraint("CK_Devices_MachineCode", "[MachineCode] > 0"));
        });

        // Board
        modelBuilder.Entity<BoardEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProtocolAddress).IsUnique();
            // BR-005: max 1 primary board per device
            entity.HasIndex(e => e.DeviceId)
                  .IsUnique()
                  .HasFilter("[IsPrimary] = 1");
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PartNumber).HasMaxLength(20);
            entity.HasOne(e => e.Device)
                  .WithMany(d => d.Boards)
                  .HasForeignKey(e => e.DeviceId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Dictionary)
                  .WithMany(d => d.Boards)
                  .HasForeignKey(e => e.DictionaryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Dictionary
        modelBuilder.Entity<DictionaryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            // BR-004: max 1 Standard dictionary in the system
            entity.HasIndex(e => e.IsStandard)
                  .IsUnique()
                  .HasFilter("[IsStandard] = 1");
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
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

            // BR-017: 2 partial indexes for uniqueness with nullable DictionaryId
            entity.HasIndex(e => new { e.VariableId, e.WordIndex, e.BitIndex })
                  .IsUnique()
                  .HasFilter("[DictionaryId] IS NULL");
            entity.HasIndex(e => new { e.VariableId, e.DictionaryId, e.WordIndex, e.BitIndex })
                  .IsUnique()
                  .HasFilter("[DictionaryId] IS NOT NULL");

            entity.Property(e => e.Meaning).HasMaxLength(200);
            // BitIndex and WordIndex must be >= 0
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_BitInterpretations_BitIndex", "[BitIndex] >= 0");
                t.HasCheckConstraint("CK_BitInterpretations_WordIndex", "[WordIndex] >= 0");
            });
            entity.HasOne(e => e.Variable)
                  .WithMany(v => v.BitInterpretations)
                  .HasForeignKey(e => e.VariableId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Dictionary)
                  .WithMany(d => d.BitInterpretations)
                  .HasForeignKey(e => e.DictionaryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Command
        modelBuilder.Entity<CommandEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CodeHigh, e.CodeLow, e.IsResponse }).IsUnique();
            // BR-016: unique command name
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            // EF Core typed JSON conversion. The column stays "ParametersJson"
            // (string, max 1000) so no schema migration is needed; the entity
            // exposes a typed List<string> instead of a raw JSON string.
            entity.Property(e => e.Parameters)
                  .HasColumnName("ParametersJson")
                  .HasMaxLength(1000)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                      v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? new List<string>());
        });

        // CommandDeviceState
        modelBuilder.Entity<CommandDeviceStateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CommandId, e.DeviceId }).IsUnique();
            entity.HasOne(e => e.Command)
                  .WithMany(c => c.DeviceStates)
                  .HasForeignKey(e => e.CommandId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // StandardVariableOverride
        modelBuilder.Entity<StandardVariableOverrideEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            // BR-010: max 1 override per (DictionaryId, StandardVariableId) pair
            entity.HasIndex(e => new { e.DictionaryId, e.StandardVariableId }).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.HasOne(e => e.Dictionary)
                  .WithMany(d => d.StandardVariableOverrides)
                  .HasForeignKey(e => e.DictionaryId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.StandardVariable)
                  .WithMany()
                  .HasForeignKey(e => e.StandardVariableId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditEntry
        modelBuilder.Entity<AuditEntryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.ChangedAt);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasOne(e => e.ChangedBy)
                  .WithMany(u => u.AuditEntries)
                  .HasForeignKey(e => e.ChangedById)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // BootstrapToken (spec 001)
        modelBuilder.Entity<BootstrapTokenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientApp).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SecretHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.SecretHash).IsUnique();
            // Invariant 1 (data-model.md): at most one Installation per consumed token.
            entity.HasIndex(e => e.ConsumedByInstallationId)
                  .IsUnique()
                  .HasFilter("[ConsumedByInstallationId] IS NOT NULL");
            entity.HasOne(e => e.ConsumedByInstallation)
                  .WithOne()
                  .HasForeignKey<BootstrapTokenEntity>(e => e.ConsumedByInstallationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Installation (spec 001)
        modelBuilder.Entity<InstallationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientApp).HasMaxLength(100).IsRequired();
            // OsUserId / MachineId are policy-required per clientApp, not
            // schema-required. Loose-policy consumers (mobile, web, headless)
            // may legitimately register without them; the per-clientApp
            // DescriptorPolicy enforces presence at the service layer.
            entity.Property(e => e.OsUserId).HasMaxLength(200);
            entity.Property(e => e.MachineId).HasMaxLength(200);
            entity.Property(e => e.AppVersion).HasMaxLength(128);
            entity.Property(e => e.DescriptorJson).IsRequired();
            entity.HasIndex(e => new { e.ClientApp, e.OsUserId, e.MachineId });
            entity.HasIndex(e => e.InstallGuid).IsUnique();
            // Spec 002 (#71): one Installation holds many credentials over
            // its lifetime — at most one Active, zero-or-more Revoked
            // historical rows. The at-most-one-Active invariant is
            // enforced by a filtered unique index in slice 2 (data-model
            // § 6).
            entity.HasMany(e => e.Credentials)
                  .WithOne(c => c.Installation)
                  .HasForeignKey(c => c.InstallationId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // InstallationApiCredential (spec 001 + 002)
        modelBuilder.Entity<InstallationApiCredentialEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SecretHash).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.InstallationId);
            entity.HasIndex(e => e.SecretHash).IsUnique();
            // Spec 002 invariant 6: at most one Active credential per
            // Installation at any instant. Enforced via a filtered unique
            // index on (InstallationId) WHERE Status = Active. The filter
            // literal references the enum ordinal — `0 = InstallationStatus.Active`
            // (Active is the first value in the enum). Reordering the
            // enum would change the filter semantics; the enum's
            // doc-comment notes the contract.
            entity.HasIndex(e => e.InstallationId)
                  .HasFilter("[Status] = 0")
                  .IsUnique()
                  .HasDatabaseName("UX_InstallationApiCredentials_Active");
        });

        // RegistrationEvent (spec 001)
        modelBuilder.Entity<RegistrationEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClaimedClientApp).HasMaxLength(100);
            entity.Property(e => e.ClaimedOsUserId).HasMaxLength(200);
            entity.Property(e => e.ClaimedMachineId).HasMaxLength(200);
            entity.Property(e => e.ClaimedAppVersion).HasMaxLength(128);
            entity.Property(e => e.SourceIp).HasMaxLength(45).IsRequired();
            entity.HasIndex(e => e.OccurredAt);
            entity.HasIndex(e => new { e.ClaimedClientApp, e.OccurredAt });
            entity.HasIndex(e => e.SourceIp);
            entity.HasOne(e => e.ResultingInstallation)
                  .WithMany()
                  .HasForeignKey(e => e.ResultingInstallationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
