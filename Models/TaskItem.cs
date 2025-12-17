using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QLDA.Models
{
    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Người được giao task
        public string? AssignedUserId { get; set; }
        [ForeignKey("AssignedUserId")]
        public ApplicationUser? AssignedUser { get; set; }

        public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }

        // Bình luận và file (tạm nullable, khởi tạo rỗng)
        public List<TaskComment>? Comments { get; set; } = new();
        public List<TaskFile>? Files { get; set; } = new();
        public List<TaskSubmission> Submissions { get; set; } = new();
    }

    public enum TaskItemStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled
    }
}
