using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLDA.Models
{
    public class TaskSubmission
    {
        [Key]
        public int Id { get; set; }

        public int TaskItemId { get; set; }
        [ForeignKey("TaskItemId")]
        public TaskItem TaskItem { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string FilePath { get; set; }
        public string FileName { get; set; }

        public string? Note { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }
}
