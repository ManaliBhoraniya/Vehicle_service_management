using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleServiceManagement.Models
{
    public class Worker
    {
        [Key]
        public int WorkerId { get; set; }

        [NotMapped]
        public int Id
        {
            get => WorkerId;
            set => WorkerId = value;
        }

        [Required]
        public int ApplicationUserId { get; set; }

        public ApplicationUser? ApplicationUser { get; set; }

        public bool IsAvailable { get; set; } = false;

        [Required]
        public string Status { get; set; } = "Pending";

        public string? ResumeFileName { get; set; }

        public string? ResumeFilePath { get; set; }

        public ICollection<ServiceAssignment> ServiceAssignments { get; set; }
            = new List<ServiceAssignment>();

        public ICollection<WorkerAvailability> Availabilities { get; set; }
            = new List<WorkerAvailability>();

        public ICollection<WorkerSpeciality> Specialities { get; set; }
            = new List<WorkerSpeciality>();
    }
}