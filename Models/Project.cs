using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QLDA.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(100)]
        public string ProjectName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [DataType(DataType.Date)]
        
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        
        public DateTime? EndDate { get; set; }

        // Người tạo dự án
        public string? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public ApplicationUser? Owner { get; set; }

        // Tính % tiến độ (dựa vào các Task)
        [NotMapped]
        public double ProgressPercent { get; set; }
        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
