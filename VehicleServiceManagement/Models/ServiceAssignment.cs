using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class ServiceAssignment
    {
        public int Id { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        [Required]
        public int WorkerId { get; set; }

        [Required]
        public DateTime ServiceDate { get; set; }

        [Required]
        public TimeSpan ServiceTime { get; set; }

        public ServiceRequest ServiceRequest { get; set; }

        public Worker Worker { get; set; }
    }
}