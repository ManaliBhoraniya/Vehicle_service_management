using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class ServiceRequest
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public Customer? Customer { get; set; }

        public Vehicle? Vehicle { get; set; }
    }
}