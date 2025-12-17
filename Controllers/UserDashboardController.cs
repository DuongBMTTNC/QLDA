using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;

namespace QLDA.Controllers
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using QLDA.Data;
    using QLDA.Models;

    public class UserDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public UserDashboardController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            ViewBag.NotificationCount = await _context.SystemNotificationUsers
   .Where(n => n.ReceiverId == user.Id && !n.IsRead)
   .CountAsync();

            var projects = await _context.ProjectMembers
                .Where(p => p.UserId == user.Id)
                .Include(p => p.Project)
                .Select(p => p.Project)
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            var model = new UserDashboardVM
            {
                User = user,
                Projects = projects,
                Notifications = notifications
            };

            return View(model);
        }
    }
}
