using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;

namespace QLDA.Models;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<TaskComment> TaskComments { get; set; }
    public DbSet<TaskFile> TaskFiles { get; set; }
    public DbSet<TaskSubmission> TaskSubmissions { get; set; }
    public DbSet<SystemNotification> SystemNotifications { get; set; }
    public DbSet<SystemNotificationUser> SystemNotificationUsers { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

        builder.Entity<SystemNotificationUser>()
       .HasOne(nu => nu.Notification)
       .WithMany(n => n.Receivers)
       .HasForeignKey(nu => nu.NotificationId)
       .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SystemNotificationUser>()
            .HasOne(nu => nu.Receiver)
            .WithMany()
            .HasForeignKey(nu => nu.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectMember>()
        .HasKey(pm => new { pm.ProjectId, pm.UserId });

        builder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.ProjectMembers)
            .HasForeignKey(pm => pm.ProjectId);

        builder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany()
            .HasForeignKey(pm => pm.UserId);

    }

}

