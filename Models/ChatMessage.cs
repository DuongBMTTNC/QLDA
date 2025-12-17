using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace QLDA.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int ProjectId { get; set; }

        [Required]
        public string SenderId { get; set; } = null!;

        [ForeignKey("SenderId")]
        public ApplicationUser? Sender { get; set; }

        [Required]
        public string Message { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
