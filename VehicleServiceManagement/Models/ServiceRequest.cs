using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleServiceManagement.Models
{
    public class ServiceRequest
    {
        [Key]
        public int ServiceRequestId { get; set; }

        // Allows existing code using .Id to continue working
        [NotMapped]
        public int Id
        {
            get => ServiceRequestId;
            set => ServiceRequestId = value;
        }

        [Required]
        public string ServiceType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime RequestDate { get; set; } = DateTime.Now;

        // Customer
        public int CustomerId { get; set; }

        public Customer? Customer { get; set; }

        // Vehicle
        public int VehicleId { get; set; }

        public Vehicle? Vehicle { get; set; }

        // Existing project property
        public string? CustomerName { get; set; }
    }
}