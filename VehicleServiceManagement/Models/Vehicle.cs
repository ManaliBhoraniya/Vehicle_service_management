using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public string VehicleNumber { get; set; } = string.Empty;

        [Required]
        public string VehicleModel { get; set; } = string.Empty;

        [Required]
        public string VehicleType { get; set; } = string.Empty;

        public Customer? Customer { get; set; }
    }
}