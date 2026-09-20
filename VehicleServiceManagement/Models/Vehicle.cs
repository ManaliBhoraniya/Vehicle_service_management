using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public string VehicleNumber { get; set; }

        [Required]
        public string VehicleModel { get; set; }

        [Required]
        public string VehicleType { get; set; }

        public Customer Customer { get; set; }
    }
}