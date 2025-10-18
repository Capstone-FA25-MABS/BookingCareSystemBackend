using BookingCare.Services.ServiceMedical.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.ServiceMedical.Data
{
    public class ServiceMedicalDbContext : DbContext
    {
        public ServiceMedicalDbContext(DbContextOptions<ServiceMedicalDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<ServiceCategoryEntity> ServiceCategories { get; set; }
        public DbSet<ServiceEntity> Services { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure ServiceCategory self-referencing relationship
            modelBuilder.Entity<ServiceCategoryEntity>()
                .HasOne(sc => sc.Parent)
                .WithMany(sc => sc.Children)
                .HasForeignKey(sc => sc.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete for self-referencing

            // Configure Service -> ServiceCategory relationship
            modelBuilder.Entity<ServiceEntity>()
                .HasOne(s => s.ServiceCategory)
                .WithMany(sc => sc.Services)
                .HasForeignKey(s => s.ServiceCategoryId)
                .OnDelete(DeleteBehavior.SetNull); // Match SQL schema



            // Configure check constraints for status fields
            modelBuilder.Entity<ServiceCategoryEntity>()
                .HasCheckConstraint("CK_ServiceCategory_Status", "status IN ('ACTIVE', 'INACTIVE')");

            modelBuilder.Entity<ServiceEntity>()
                .HasCheckConstraint("CK_Service_Status", "status IN ('ACTIVE', 'INACTIVE')");


            // Configure default values
            modelBuilder.Entity<ServiceCategoryEntity>()
                .Property(sc => sc.Status)
                .HasDefaultValue("INACTIVE");

            modelBuilder.Entity<ServiceEntity>()
                .Property(s => s.Status)
                .HasDefaultValue("INACTIVE");



            // Configure Guid primary keys to use NEWID() for SQL Server
            modelBuilder.Entity<ServiceCategoryEntity>()
                .Property(sc => sc.Id)
                .HasDefaultValueSql("NEWID()");

            modelBuilder.Entity<ServiceEntity>()
                .Property(s => s.Id)
                .HasDefaultValueSql("NEWID()");



            // Configure decimal precision for price fields
            modelBuilder.Entity<ServiceEntity>()
                .Property(s => s.Price)
                .HasPrecision(10, 2);

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
            // Update timestamps for entities that have UpdatedAt property
            // Currently no entities require timestamp updates
        }
    }
}
