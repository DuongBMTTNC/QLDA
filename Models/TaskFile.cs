using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QLDA.Models
{
    public class TaskFile
    {
        [Key]
        public int Id { get; set; }

        public int TaskItemId { get; set; }
        [ForeignKey("TaskItemId")]
        public TaskItem TaskItem { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string FilePath { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public string? UploadedById { get; set; }
        [ForeignKey("UploadedById")]
        public ApplicationUser? UploadedBy { get; set; }
    }
}
