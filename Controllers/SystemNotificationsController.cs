using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using QLDA.Models;
using QLDA.Services;

public class SystemNotificationsController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EmailService _emailService;

    public SystemNotificationsController(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        EmailService emailService)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
    }

    // GET: Admin tạo thông báo
    public async Task<IActionResult> Create()
    {
        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    // POST: Gửi thông báo
    [HttpPost]
    public async Task<IActionResult> Create(string title, string message, List<string> receivers)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
        {
            TempData["error"] = "Vui lòng nhập đầy đủ tiêu đề và nội dung.";
            return RedirectToAction("Create");
        }

        var sender = await _userManager.GetUserAsync(User);

        // --- Tạo thông báo ---
        var notification = new SystemNotification
        {
            Title = title,
            Message = message,
            SenderId = sender.Id,
            CreatedAt = DateTime.Now
        };

        _context.SystemNotifications.Add(notification);
        await _context.SaveChangesAsync();

        // Nếu không chọn user → gửi cho tất cả
        if (receivers == null || receivers.Count == 0)
            receivers = await _context.Users.Select(u => u.Id).ToListAsync();

        // --- Tạo danh sách người nhận ---
        foreach (var userId in receivers)
        {
            _context.SystemNotificationUsers.Add(new SystemNotificationUser
            {
                NotificationId = notification.Id,
                ReceiverId = userId,
                IsRead = false
            });

            // Gửi email
            var user = await _context.Users.FindAsync(userId);
            await _emailService.SendEmailAsync(
                user.Email,
                $"Thông báo hệ thống: {title}",
                message
            );
        }

        await _context.SaveChangesAsync();

        TempData["success"] = "Gửi thông báo thành công!";
        return RedirectToAction("Create");
    }

    public async Task<IActionResult> MyNotifications()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var notifications = await _context.SystemNotificationUsers
            .Where(nu => nu.ReceiverId == user.Id)
            .Include(nu => nu.Notification)
            .OrderByDescending(nu => nu.Notification.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        var item = await _context.SystemNotificationUsers
            .FirstOrDefaultAsync(n => n.NotificationId == id && n.ReceiverId == user.Id);

        if (item == null)
            return NotFound();

        item.IsRead = true;
        await _context.SaveChangesAsync();

        return RedirectToAction("MyNotifications");
    }
}
