using BookingCare.Services.Auth.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Auth.Data;

public class AuthDbContext : IdentityDbContext<AccountEntity, RoleEntity, Guid>
{
    public DbSet<PermissionEntity> Permissions { get; set; }
    public DbSet<RolePermissionEntity> RolePermissions { get; set; }
    public DbSet<AccountRoleEntity> AccountRoles { get; set; }
    public DbSet<RefreshTokenEntity> RefreshTokens { get; set; }
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ApplicationUser
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Unique constraints
            entity.HasIndex(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.PhoneNumber).IsUnique();
        });

        // Configure ApplicationRole
        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Unique constraint
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure ApplicationUserRole
        modelBuilder.Entity<AccountRoleEntity>(entity =>
        {
            // Key is already configured in base class IdentityUserRole<Guid>
            // We only need to configure additional properties

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        // Configure Permission
        modelBuilder.Entity<PermissionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Unique constraint
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Configure RolePermission
        modelBuilder.Entity<RolePermissionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Relationships
            entity.HasOne(e => e.Role)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint to prevent duplicate role-permission combinations
            entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
        });

        // Configure RefreshToken
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .HasMaxLength(450)
                .IsRequired();    

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETDATE()");

            // Relationships
            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance and security
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.AccountId);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is AccountEntity || e.Entity is RoleEntity || 
                       e.Entity is AccountRoleEntity || 
                       e.Entity is PermissionEntity || e.Entity is RolePermissionEntity ||
                       e.Entity is RefreshTokenEntity);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is AccountEntity user)
                {
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is RoleEntity role)
                {
                    role.CreatedAt = DateTime.UtcNow;
                    role.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is AccountRoleEntity userRole)
                {
                    userRole.CreatedAt = DateTime.UtcNow;
                    userRole.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is PermissionEntity permission)
                {
                    permission.CreatedAt = DateTime.UtcNow;
                    permission.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is RolePermissionEntity rolePermission)
                {
                    rolePermission.CreatedAt = DateTime.UtcNow;
                    rolePermission.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is AccountEntity user)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(user.CreatedAt)).IsModified = false;
                }
                else if (entry.Entity is RoleEntity role)
                {
                    role.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(role.CreatedAt)).IsModified = false;
                }
                else if (entry.Entity is AccountRoleEntity userRole)
                {
                    userRole.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(userRole.CreatedAt)).IsModified = false;
                }     
                else if (entry.Entity is PermissionEntity permission)
                {
                    permission.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(permission.CreatedAt)).IsModified = false;
                }
                else if (entry.Entity is RolePermissionEntity rolePermission)
                {
                    rolePermission.UpdatedAt = DateTime.UtcNow;
                    entry.Property(nameof(rolePermission.CreatedAt)).IsModified = false;
                }
            }
        }
    }
}
