using Microsoft.AspNetCore.Identity;

namespace QLDA.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
