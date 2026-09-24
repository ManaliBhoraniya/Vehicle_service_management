using System.ComponentModel.DataAnnotations;

namespace VehicleServiceManagement.Models
{
    public class WorkerAvailability
    {
        [Key]
        public int WorkerAvailabilityId { get; set; }

        [Required]
        public int WorkerId { get; set; }

        public Worker? Worker { get; set; }

        [Required]
        public string DayOfWeek { get; set; } = string.Empty;

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }

        public bool IsAvailable { get; set; }
    }
}