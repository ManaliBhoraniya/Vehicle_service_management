using Microsoft.AspNetCore.Identity;

namespace VehicleServiceManagement.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string Name { get; set; } = string.Empty;
    }
}