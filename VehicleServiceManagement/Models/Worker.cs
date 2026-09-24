using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleServiceManagement.Models
{
    public class Worker
    {
        [Key]
        public int WorkerId { get; set; }

        // Compatibility for existing views/controllers
        [NotMapped]
        public int Id
        {
            get => WorkerId;
            set => WorkerId = value;
        }

        // Relationship with ApplicationUser
        [Required]
        public int ApplicationUserId { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public string Profession { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        // Resume
        public string? ResumeFileName { get; set; }

        public string? ResumeFilePath { get; set; }

        // Assigned services
        public ICollection<ServiceAssignment> ServiceAssignments { get; set; }
            = new List<ServiceAssignment>();

        // Weekly availability
        public ICollection<WorkerAvailability> Availabilities { get; set; }
            = new List<WorkerAvailability>();
    }
}