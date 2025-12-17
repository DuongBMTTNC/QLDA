namespace QLDA.Models
{
    public class SystemNotification
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }

        public ICollection<SystemNotificationUser> Receivers { get; set; }
    }
}
