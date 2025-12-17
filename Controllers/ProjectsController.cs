using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;
using QLDA.Services;

namespace QLDA.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public ProjectsController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {

            var user = await _userManager.GetUserAsync(User);

            // Lấy các dự án user làm Owner
            var ownerProjects = _context.Projects
                .Where(p => p.OwnerId == user.Id);

            // Lấy các dự án user được add vào (Member)
            var memberProjects = _context.ProjectMembers
                .Where(m => m.UserId == user.Id)
                .Select(m => m.Project);

            // Gộp 2 danh sách lại, tránh trùng
            var allProjects = await ownerProjects
                .Union(memberProjects)
                .ToListAsync();

            return View(allProjects);
        }

        // GET: Projects/Create
        public IActionResult Create()
        {
            if (TempData["FormData"] != null)
            {
                var json = TempData["FormData"]!.ToString();
                var model = System.Text.Json.JsonSerializer.Deserialize<Project>(json!);
                return View(model);
            }

            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (project.StartDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Ngày bắt đầu phải là hôm nay hoặc sau hôm nay (không được trong quá khứ).";
                TempData["FormData"] = System.Text.Json.JsonSerializer.Serialize(project);
                return RedirectToAction("Index");
            }

            // ❌ Ngày kết thúc phải >= ngày bắt đầu
            if (project.EndDate.HasValue && project.EndDate.Value.Date < project.StartDate.Date)
            {
                TempData["ErrorMessage"] = "Ngày kết thúc phải sau hoặc cùng ngày với ngày bắt đầu.";
                TempData["FormData"] = System.Text.Json.JsonSerializer.Serialize(project);
                return RedirectToAction("Index");
            }


            // 🟢 Không có lỗi → lưu dự án
            var user = await _userManager.GetUserAsync(User);
            project.OwnerId = user.Id;

            // Tạo ProjectMember và thêm vào collection
            project.ProjectMembers = new List<ProjectMember>
{
    new ProjectMember
    {
        UserId = user.Id,
        Role = "Owner"
    }
};

            // Lưu project cùng ProjectMember
            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            // 📧 Gửi email xác nhận thành công
            if (!string.IsNullOrEmpty(user.Email))
            {
                string subject = "Xác nhận tạo dự án thành công";
                string body = $@"
            <h3>Xin chào {user.FullName ?? user.UserName},</h3>
            <p>Bạn vừa tạo dự án <b>{project.ProjectName}</b> thành công!</p>
            <p><b>Ngày bắt đầu:</b> {project.StartDate:dd/MM/yyyy}</p>
            <p><b>Ngày kết thúc:</b> {(project.EndDate?.ToString("dd/MM/yyyy") ?? "(Chưa đặt)")}</p>
            <br/>
            <p>Trân trọng,<br/>Hệ thống Quản lý Dự án</p>
        ";
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }

            TempData["SuccessMessage"] = "Tạo dự án thành công!";
            return RedirectToAction(nameof(Index));
        }


        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Owner)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedUser)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Files)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Comments)
                        .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.ProjectId == id);
            var user = await _userManager.GetUserAsync(User);

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == id && m.UserId == user.Id);

            if (project == null)
                return NotFound();

            // Tính tiến độ: % task Completed
            double progress = 0;
            if (project.Tasks.Any())
            {
                progress = project.Tasks.Count(t => t.Status == TaskItemStatus.Completed) * 100.0 / project.Tasks.Count;
            }

            var vm = new ProjectDetailsViewModel
            {
                Project = project,
                Tasks = project.Tasks.OrderBy(t => t.DueDate).ToList(),
                Members = project.ProjectMembers.ToList(),
                ProgressPercent = Math.Round(progress, 2),
                CurrentUserId = user.Id,
                CurrentUserRole =
        project.OwnerId == user.Id ? "Owner" :
        member?.Role ?? "Member"

            };
            if (vm.CurrentUserRole != "Owner" && vm.CurrentUserRole != "Manager")
            {
                vm.Tasks = vm.Tasks
                    .Where(t => t.AssignedUserId == vm.CurrentUserId)
                    .ToList();
            }

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> InviteMember(int projectId, string email, string role)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                TempData["InviteMessage"] = "Dự án không tồn tại.";
                return RedirectToAction("Details", new { id = projectId });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // ✅ Chỉ Owner hoặc Manager mới được mời thành viên
            var memberRole = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId && pm.UserId == currentUser.Id)
                .Select(pm => pm.Role)
                .FirstOrDefaultAsync();

            if (memberRole != "Owner" && memberRole != "Manager")
            {
                TempData["InviteMessage"] = "Bạn không có quyền mời thành viên.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // tìm user theo Email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["InviteMessage"] = "Không tồn tại tài khoản với email này.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // kiểm tra xem đã là thành viên chưa
            bool exists = await _context.ProjectMembers
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == user.Id);

            if (exists)
            {
                TempData["InviteMessage"] = "Người này đã là thành viên của dự án.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // thêm vào ProjectMembers
            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = user.Id,
                Role = role
            };

            _context.ProjectMembers.Add(member);

            var notification = new SystemNotification
            {
                Title = $"Bạn đã được mời vào dự án {project.ProjectName}",
                Message = $"Người dùng {currentUser.UserName} đã mời bạn tham gia dự án {project.ProjectName}.",
                CreatedAt = DateTime.Now,
                SenderId = currentUser.Id,
                Receivers = new List<SystemNotificationUser>
        {
            new SystemNotificationUser
            {
                ReceiverId = user.Id,
                IsRead = false
            }
        }
            };
            _context.SystemNotifications.Add(notification);

            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(
    email, // địa chỉ email người nhận
    $"Bạn đã được mời vào dự án {project.ProjectName}", // tiêu đề email
    "Hãy đăng nhập để xem chi tiết." // nội dung email
);
            TempData["SuccessMessage"] = "Đã thêm thành viên thành công!";
            return RedirectToAction("Details", new { id = projectId });
        }

        public async Task<IActionResult> Chat(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isOwner = currentUser != null && project.OwnerId == currentUser.Id;
            var isMember = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == id && pm.UserId == currentUser.Id);

            if (!isOwner && !isMember) return Forbid();

            // Lấy 50 tin nhắn gần nhất của project (theo thời gian tăng dần)
            var messages = await _context.ChatMessages
                .Where(c => c.ProjectId == id)
                .Include(c => c.Sender)
                .OrderByDescending(c => c.SentAt)
                .Take(50)
                .OrderBy(c => c.SentAt)
                .ToListAsync();

            ViewBag.ProjectId = id;
            ViewBag.ProjectName = project.ProjectName;
            return View(messages); // Views/Projects/Chat.cshtml
        }
        public async Task<IActionResult> Progress(int id)
        {
            var project = await _context.Projects
       .Include(p => p.Tasks)
       .ThenInclude(t => t.AssignedUser)
       .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
                return NotFound();

            // ---- Tính tổng task ----
            int totalTasks = project.Tasks.Count;
            int completedTasks = project.Tasks.Count(t => t.Status == TaskItemStatus.Completed);

            int progressPercent = totalTasks > 0
                ? (completedTasks * 100 / totalTasks)
                : 0;

            // ---- Tính thời gian ----
            int totalDays = (project.EndDate.HasValue && project.StartDate != null)
                ? (int)(project.EndDate.Value - project.StartDate).TotalDays
                : 0;

            int daysPassed = (int)(DateTime.Now - project.StartDate).TotalDays;

            int daysLeft = project.EndDate.HasValue
                ? (int)(project.EndDate.Value - DateTime.Now).TotalDays
                : 0;

            double timePercent = totalDays > 0
                ? Math.Round((double)daysPassed / totalDays * 100, 1)
                : 0;

            // ---- Tính thống kê theo thành viên ----
            var memberStats = project.Tasks
    .GroupBy(t => t.AssignedUserId)
    .Select(g => new MemberTaskStats
    {
        UserId = g.Key ?? "none",

        UserName = g.FirstOrDefault()?.AssignedUser?.UserName ?? "Chưa giao",

        FullName = g.FirstOrDefault()?.AssignedUser?.FullName ??
                   g.FirstOrDefault()?.AssignedUser?.UserName ?? "Chưa giao",

        AvatarUrl = g.FirstOrDefault()?.AssignedUser?.AvatarUrl ??
                    "/images/default-avatar.png",

        CompletedTasks = g.Where(t => t.Status == TaskItemStatus.Completed).ToList(),

        PendingTasks = g.Where(t => t.Status != TaskItemStatus.Completed).ToList()
    })
    .ToList();

            // ---- Tạo ViewModel ----
            var vm = new ProjectProgressViewModel
            {
                Project = project,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                ProgressPercent = progressPercent,
                TotalDays = totalDays,
                DaysPassed = daysPassed,
                DaysLeft = daysLeft,
                TimePercent = timePercent,

                // ⬅ THÊM FLAG CẢNH BÁO
                IsBehindSchedule = progressPercent < timePercent,

                // ⬅ THÊM THỐNG KÊ THÀNH VIÊN
                MemberStats = memberStats

            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> SearchUser(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<object>());

            var result = await _context.Users
                .Where(u => u.Email.Contains(keyword) || u.UserName.Contains(keyword))
                .Select(u => new {
                    u.Id,
                    u.Email,
                    Name = u.Email ?? u.UserName
                })
                .Take(5)
                .ToListAsync();

            return Json(result);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            // chỉ Owner được sửa
            var currentUser = await _userManager.GetUserAsync(User);
            if (project.OwnerId != currentUser.Id) return Forbid();

            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.ProjectId) return NotFound();

            var existing = await _context.Projects.FindAsync(id);
            if (existing == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (existing.OwnerId != currentUser.Id) return Forbid();

            if (ModelState.IsValid)
            {
                existing.ProjectName = project.ProjectName;
                existing.Description = project.Description;
                existing.StartDate = project.StartDate;
                existing.EndDate = project.EndDate;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật dự án thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Files)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.Comments)
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (project.OwnerId != currentUser.Id) return Forbid();

            // Xóa comment
            _context.TaskComments.RemoveRange(
                project.Tasks.SelectMany(t => t.Comments)
            );

            // Xóa file trong server nếu có
            foreach (var file in project.Tasks.SelectMany(t => t.Files))
            {
                if (System.IO.File.Exists(file.FilePath))
                    System.IO.File.Delete(file.FilePath);
            }

            // Xóa file record
            _context.TaskFiles.RemoveRange(
                project.Tasks.SelectMany(t => t.Files)
            );

            // Xóa Task
            _context.TaskItems.RemoveRange(project.Tasks);

            // Xóa thành viên
            _context.ProjectMembers.RemoveRange(project.ProjectMembers);

            // Xóa chat messages
            var chatMessages = await _context.ChatMessages
    .Where(c => c.ProjectId == project.ProjectId)
    .ToListAsync();
            _context.ChatMessages.RemoveRange(chatMessages);

            // Xóa project
            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa dự án và toàn bộ dữ liệu liên quan!";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> ChangeMemberRole(int projectId, string userId, string newRole)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null) { TempData["ErrorMessage"] = "Không tìm thấy dự án!"; return RedirectToAction("Details", new { id = projectId }); }

                var currentUser = await _userManager.GetUserAsync(User);
                if (project.OwnerId != currentUser.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền thay đổi vai trò thành viên!";
                    return RedirectToAction("Details", new { id = projectId });
                }

                var member = project.ProjectMembers.FirstOrDefault(m => m.UserId == userId);
                if (member == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thành viên trong dự án!";
                    return RedirectToAction("Details", new { id = projectId });
                }

                var targetUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

                string oldRole = member.Role;
                member.Role = newRole;
                var notification = new SystemNotification
                {
                    Title = "Cập nhật vai trò trong dự án",
                    Message = $"Bạn đã được thay đổi vai trò từ **{oldRole}** → **{newRole}** trong dự án **{project.ProjectName}** bởi {currentUser.FullName}.",
                    SenderId = currentUser.Id,
                    CreatedAt = DateTime.Now,
                    Receivers = new List<SystemNotificationUser>()
                };

                notification.Receivers.Add(new SystemNotificationUser
                {
                    ReceiverId = targetUser.Id
                });

                _context.SystemNotifications.Add(notification);
                await _context.SaveChangesAsync();

                string subject = $"Thay đổi vai trò trong dự án {project.ProjectName}";
                string body =
                    $"Xin chào {targetUser.FullName},<br><br>" +
                    $"Vai trò của bạn trong dự án <b>{project.ProjectName}</b> đã thay đổi:<br>" +
                    $"👉 <b>{oldRole}</b> → <b>{newRole}</b><br><br>" +
                    $"Thay đổi được thực hiện bởi: <b>{currentUser.FullName}</b><br>" +
                    $"Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}<br><br>" +
                    $"Trân trọng!";

                await _emailService.SendEmailAsync(targetUser.Email, subject, body);

                TempData["SuccessMessage"] = "Cập nhật vai trò thành công!";
                return RedirectToAction("Details", new { id = projectId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("Details", new { id = projectId });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RemoveMember(int projectId, string userId)
        {
            try
            {
                // 1. Kiểm tra đầu vào
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "UserId không hợp lệ!";
                    return RedirectToAction("Details", new { id = projectId });
                }

                // 2. Tìm project
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy dự án!";
                    return RedirectToAction("Index");
                }

                // 3. Kiểm tra quyền
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Unauthorized();
                }

                if (project.OwnerId != currentUser.Id)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa thành viên khỏi dự án!";
                    return Forbid();
                }

                // 4. Không cho phép xóa chính chủ dự án
                if (userId == project.OwnerId)
                {
                    TempData["ErrorMessage"] = "Không thể xóa chủ dự án!";
                    return RedirectToAction("Details", new { id = projectId });
                }

                // 5. Tìm member
                var member = project.ProjectMembers.FirstOrDefault(m => m.UserId == userId);
                if (member == null)
                {
                    TempData["ErrorMessage"] = "Thành viên không tồn tại trong dự án!";
                    return RedirectToAction("Details", new { id = projectId });
                }

                // 6. Xóa
                _context.ProjectMembers.Remove(member);

                // 7. Lưu thay đổi
                
                await _context.SaveChangesAsync();
                

                TempData["SuccessMessage"] = "Đã xóa thành viên khỏi dự án!";
                return RedirectToAction("Details", new { id = projectId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi không xác định!";
                return RedirectToAction("Details", new { id = projectId });
            }
        }

        // (Các action Create/Index/...) giữ nguyên như bạn đã có
    }
    public class ProjectDetailsViewModel
    {
        public Project Project { get; set; } = null!;
        public List<TaskItem> Tasks { get; set; } = new();
        public List<ProjectMember> Members { get; set; } = new();
        public double ProgressPercent { get; set; }
        public string CurrentUserId { get; set; } = string.Empty;
        public string CurrentUserRole { get; set; }
    }
    public class MemberTaskStats
    {
        public string UserId { get; set; }

        public string UserName { get; set; }      // username / email
        public string FullName { get; set; }      // fullname
        public string AvatarUrl { get; set; }     // avatar

        public List<TaskItem> CompletedTasks { get; set; } = new();
        public List<TaskItem> PendingTasks { get; set; } = new();
    }
    public class ProjectProgressViewModel
    {
        public Project Project { get; set; }

        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int ProgressPercent { get; set; }

        public int TotalDays { get; set; }
        public int DaysPassed { get; set; }
        public int DaysLeft { get; set; }
        public double TimePercent { get; set; }
        public bool IsBehindSchedule { get; set; }
        public List<MemberTaskStats> MemberStats { get; set; } = new();
       
    }
}

