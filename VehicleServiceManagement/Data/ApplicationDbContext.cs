using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Worker> Workers { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ServiceAssignment> ServiceAssignments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Customer)
                .WithMany()
                .HasForeignKey(sr => sr.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Vehicle)
                .WithMany()
                .HasForeignKey(sr => sr.VehicleId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}