namespace QLDA.Models
{
    public class SystemNotificationUser
    {
        public int Id { get; set; }

        public int NotificationId { get; set; }
        public SystemNotification Notification { get; set; }

        public string ReceiverId { get; set; }
        public ApplicationUser Receiver { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
