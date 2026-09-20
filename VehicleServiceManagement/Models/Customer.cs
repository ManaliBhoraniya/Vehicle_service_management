using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationUserId { get; set; }

        [Required]
        public string Phone { get; set; }

        public ApplicationUser ApplicationUser { get; set; }
    }
}