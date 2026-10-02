using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class WorkerSpeciality
    {
        [Key]
        public int WorkerSpecialityId { get; set; }

        [Required]
        public int WorkerId { get; set; }

        public Worker? Worker { get; set; }

        [Required]
        public string Speciality { get; set; } = string.Empty;
    }
}