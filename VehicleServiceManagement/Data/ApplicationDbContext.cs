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

        public DbSet<Worker> Workers { get; set; }

        public DbSet<WorkerAvailability> WorkerAvailabilities
        {
            get; set;
        }

        public DbSet<ServiceAssignment> ServiceAssignments
        {
            get; set;
        }

        public DbSet<ServiceRequest> ServiceRequests
        {
            get; set;
        }

        public DbSet<Customer> Customers
        {
            get; set;
        }

        public DbSet<Vehicle> Vehicles
        {
            get; set;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Worker -> ApplicationUser
            builder.Entity<Worker>()
                .HasOne(w => w.ApplicationUser)
                .WithOne(u => u.Worker)
                .HasForeignKey<Worker>(w => w.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Customer -> ApplicationUser
            builder.Entity<Customer>()
                .HasOne(c => c.ApplicationUser)
                .WithMany()
                .HasForeignKey(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkerAvailability -> Worker
            builder.Entity<WorkerAvailability>()
                .HasOne(a => a.Worker)
                .WithMany(w => w.Availabilities)
                .HasForeignKey(a => a.WorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ServiceRequest -> Customer
            builder.Entity<ServiceRequest>()
                .HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ServiceRequest -> Vehicle
            builder.Entity<ServiceRequest>()
                .HasOne(s => s.Vehicle)
                .WithMany()
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ServiceAssignment -> Worker
            builder.Entity<ServiceAssignment>()
                .HasOne(sa => sa.Worker)
                .WithMany(w => w.ServiceAssignments)
                .HasForeignKey(sa => sa.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ServiceAssignment -> ServiceRequest
            builder.Entity<ServiceAssignment>()
                .HasOne(sa => sa.ServiceRequest)
                .WithMany()
                .HasForeignKey(sa => sa.ServiceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}