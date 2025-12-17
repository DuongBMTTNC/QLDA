using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QLDA.Models
{
    public class ProjectMember
    {
        [Key, Column(Order = 0)]
        public int ProjectId { get; set; }

        [Key, Column(Order = 1)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ProjectId))]
        public Project? Project { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // Vai trò trong dự án (Owner, Member, Viewer, ...)
        public string Role { get; set; } = "Member";
    }
}
