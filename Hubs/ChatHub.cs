using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;
using System.Text.RegularExpressions;

namespace QLDA.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Client gọi để join group dựa trên projectId
        public async Task JoinProjectGroup(int projectId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) throw new HubException("Unauthorized");

            // kiểm tra quyền: owner hoặc member
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new HubException("Project not found");

            var isOwner = project.OwnerId == user.Id;
            var isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == user.Id);

            if (!isOwner && !isMember)
                throw new HubException("Bạn không có quyền truy cập chat của project này");

            string groupName = GetGroupName(projectId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // Client gửi tin nhắn
        public async Task SendMessageToProject(int projectId, string message)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null) throw new HubException("Unauthorized");

            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) throw new HubException("Project not found");

            var isOwner = project.OwnerId == user.Id;
            var isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == user.Id);

            if (!isOwner && !isMember)
                throw new HubException("Bạn không có quyền gửi tin nhắn cho project này");

            var chat = new ChatMessage
            {
                ProjectId = projectId,
                SenderId = user.Id,
                Message = message,
                SentAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(chat);
            await _context.SaveChangesAsync();

            string groupName = GetGroupName(projectId);

            // Gửi tới tất cả clients trong group (có thể gửi cả userId, senderName, sentAt)
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new
            {
                id = chat.Id,
                projectId = chat.ProjectId,
                senderId = user.Id,
                senderName = user.FullName ?? user.UserName,
                avatar = user.AvatarUrl ?? "/images/default-avatar.png",
                message = chat.Message,
                sentAt = chat.SentAt
            });
        }

        private string GetGroupName(int projectId) => $"Project_{projectId}";
    }
}
