using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleServiceManagement.Models
{
    public class ServiceAssignment
    {
        [Key]
        public int ServiceAssignmentId { get; set; }

        // Compatibility for existing views/controllers
        [NotMapped]
        public int Id
        {
            get => ServiceAssignmentId;
            set => ServiceAssignmentId = value;
        }

        [Required]
        public int WorkerId { get; set; }

        public Worker? Worker { get; set; }

        [Required]
        public int ServiceRequestId { get; set; }

        public ServiceRequest? ServiceRequest { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        public DateTime? CompletedDate { get; set; }

        public string? Notes { get; set; }


        // =====================================================
        // Compatibility properties for existing views
        // =====================================================

        [NotMapped]
        public DateTime ServiceDate
        {
            get => AssignedDate.Date;

            set
            {
                AssignedDate =
                    value.Date.Add(AssignedDate.TimeOfDay);
            }
        }

        [NotMapped]
        public TimeSpan ServiceTime
        {
            get => AssignedDate.TimeOfDay;

            set
            {
                AssignedDate =
                    AssignedDate.Date.Add(value);
            }
        }
    }
}