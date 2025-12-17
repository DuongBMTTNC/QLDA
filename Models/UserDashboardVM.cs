namespace QLDA.Models
{
    public class UserDashboardVM
    {
        public ApplicationUser User { get; set; }
        public List<Project> Projects { get; set; }
        public List<Notification> Notifications { get; set; }
    }
}
