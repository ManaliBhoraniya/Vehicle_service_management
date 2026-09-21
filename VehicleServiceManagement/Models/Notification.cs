using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        public Customer? Customer { get; set; }

        public ServiceRequest? ServiceRequest { get; set; }
    }
}