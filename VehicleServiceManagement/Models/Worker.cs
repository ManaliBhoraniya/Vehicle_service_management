using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class Worker
    {
        public int Id { get; set; }

        [Required]
        public int ApplicationUserId { get; set; }

        [Required]
        public string Profession { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }
    }
}