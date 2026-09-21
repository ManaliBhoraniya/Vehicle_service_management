using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationUserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        public ApplicationUser? ApplicationUser { get; set; }

        public ICollection<Vehicle> Vehicles { get; set; }
            = new List<Vehicle>();
    }
}