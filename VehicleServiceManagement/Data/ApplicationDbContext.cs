using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleServiceManagement.Models;

namespace VehicleServiceManagement.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Worker> Workers { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<ServiceAssignment> ServiceAssignments { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer -> ApplicationUser
            modelBuilder.Entity<Customer>()
                .HasOne(c => c.ApplicationUser)
                .WithMany()
                .HasForeignKey(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer -> Vehicles
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Customer)
                .WithMany(c => c.Vehicles)
                .HasForeignKey(v => v.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Worker -> ApplicationUser
            modelBuilder.Entity<Worker>()
                .HasOne(w => w.ApplicationUser)
                .WithMany()
                .HasForeignKey(w => w.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ServiceRequest -> Customer
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Customer)
                .WithMany()
                .HasForeignKey(sr => sr.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // ServiceRequest -> Vehicle
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Vehicle)
                .WithMany()
                .HasForeignKey(sr => sr.VehicleId)
                .OnDelete(DeleteBehavior.NoAction);

            // ServiceAssignment -> ServiceRequest
            modelBuilder.Entity<ServiceAssignment>()
                .HasOne(sa => sa.ServiceRequest)
                .WithMany()
                .HasForeignKey(sa => sa.ServiceRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            // ServiceAssignment -> Worker
            modelBuilder.Entity<ServiceAssignment>()
                .HasOne(sa => sa.Worker)
                .WithMany()
                .HasForeignKey(sa => sa.WorkerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Notification -> Customer
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Customer)
                .WithMany()
                .HasForeignKey(n => n.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Notification -> ServiceRequest
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.ServiceRequest)
                .WithMany()
                .HasForeignKey(n => n.ServiceRequestId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}