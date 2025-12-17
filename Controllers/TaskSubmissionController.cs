using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;

namespace QLDA.Controllers
{
    [Authorize]
    public class TaskSubmissionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TaskSubmissionController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Preview(int id)
        {
            var submission = await _context.TaskSubmissions
                .Include(t => t.TaskItem)
                    .ThenInclude(t => t.Project)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (submission == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            bool isSubmitter = submission.UserId == userId;
            bool isProjectMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == submission.TaskItem.ProjectId
                             && pm.UserId == userId);

            if (!isSubmitter && !isProjectMember)
                return Forbid();

            return View(submission);
        }

        public async Task<IActionResult> Download(int id)
        {
            var submission = await _context.TaskSubmissions.FindAsync(id);
            if (submission == null) return NotFound();

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                submission.FilePath.TrimStart('/')
            );

            var bytes = await System.IO.File.ReadAllBytesAsync(path);
            return File(bytes, "application/octet-stream", submission.FileName);
        }
    }

}
