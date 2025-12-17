using System.ComponentModel.DataAnnotations;

namespace QLDA.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = default!;  // FK -> AspNetUsers.Id

        [Required, MaxLength(200)]
        public string Title { get; set; } = default!;

        [Required, MaxLength(1000)]
        public string Body { get; set; } = default!;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
