
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;
using QLDA.Services;

namespace QLDA.Controllers
{
    public class TaskItemsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public TaskItemsController(AppDbContext context,
                                  UserManager<ApplicationUser> userManager,
                                  EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: TaskItems/Create?projectId=1
        public async Task<IActionResult> Create(int projectId)
        {
            
            var currentUser = await _userManager.GetUserAsync(User);
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                return NotFound();

            var member = project.ProjectMembers
       .FirstOrDefault(pm => pm.UserId == currentUser.Id);

            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
                return Forbid(); // không có quyền
            // Danh sách member
            var members = project.ProjectMembers
                .Select(pm => pm.User!)
                .ToList();

            ViewBag.MemberList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                members.Select(u => new { u.Id, Name = u.FullName ?? u.UserName }),
                "Id",
                "Name"
            );

            var task = new TaskItem { ProjectId = projectId };
            return View(task);
        }

        // POST: TaskItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem task)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var member = await _context.ProjectMembers
        .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId
                                && pm.UserId == currentUser.Id);

            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền tạo task!";
                return Forbid(); // không có quyền tạo task
            }
            if (!ModelState.IsValid)
            {
                var project = await _context.Projects
                    .Include(p => p.ProjectMembers)
                        .ThenInclude(pm => pm.User)
                    .FirstOrDefaultAsync(p => p.ProjectId == task.ProjectId);

                var members = project?.ProjectMembers.Select(pm => pm.User!).ToList() ?? new List<ApplicationUser>();

                ViewBag.MemberList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                    members.Select(u => new { u.Id, Name = u.FullName ?? u.UserName }),
                    "Id",
                    "Name"
                );

                return View(task);
            }
            if (task.DueDate.HasValue && task.DueDate.Value <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Hạn nộp phải sau ngày tạo.";
                return RedirectToAction("Create", new { projectId = task.ProjectId });
            }


            task.CreatedAt = DateTime.Now;
            task.Status = TaskItemStatus.Pending;
            

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();

            var assignedUser = await _userManager.FindByIdAsync(task.AssignedUserId);
            if (assignedUser != null && !string.IsNullOrEmpty(assignedUser.Email))
            {
                        string subject = $"Bạn được giao một Task mới: {task.Title}";
                        string message = $@"
                Xin chào {assignedUser.FullName ?? assignedUser.UserName},

                Bạn vừa được giao một task mới trong dự án.

                • Tiêu đề: {task.Title}
                • Mô tả: {task.Description}
                • Hạn nộp: {task.DueDate?.ToString("dd/MM/yyyy")}
        
                Vui lòng đăng nhập để xem chi tiết.

                Trân trọng !
    
                        ";

                        await _emailService.SendEmailAsync(assignedUser.Email, subject, message);
            }

            TempData["SuccessMessage"] = "Tạo task thành công!";
            return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
        }
        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.TaskItems
       .Include(t => t.Project)
           .ThenInclude(p => p.ProjectMembers)
       .Include(t => t.AssignedUser)
       .Include(t => t.Comments).ThenInclude(c => c.User)
       .Include(t => t.Submissions) // Include file submissions
           .ThenInclude(s => s.User) // Người submit
       .FirstOrDefaultAsync(t => t.Id == id);


            if (task == null) return NotFound();

            var userId = _userManager.GetUserId(User);

            var member = task.Project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == userId);

            var vm = new TaskDetailViewModel
            {
                Task = task,
                IsAssignedUser = task.AssignedUserId == userId,
                IsOwner = member?.Role == "Owner",
                IsManager = member?.Role == "Manager"
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(int taskId, IFormFile file)
        {
            var task = await _context.TaskItems
                .Include(t => t.Files)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null || file == null) return NotFound();

            // Lưu file vào wwwroot/uploads/tasks/
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/tasks");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var taskFile = new TaskFile
            {
                TaskItemId = taskId,
                FileName = file.FileName,
                FilePath = "/uploads/tasks/" + uniqueFileName,
                UploadedById = _userManager.GetUserId(User)
            };

            _context.TaskFiles.Add(taskFile);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
        }
        [HttpPost]
        public async Task<IActionResult> AddComment(int taskId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return Redirect(Request.Headers["Referer"].ToString());

            var userId = _userManager.GetUserId(User);

            var comment = new TaskComment
            {
                TaskId = taskId,
                Content = content,
                UserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            // Lấy ProjectId để redirect đúng
            var task = await _context.TaskItems.FirstAsync(t => t.Id == taskId);

            return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
        }
        [HttpPost]
        public async Task<IActionResult> SubmitTask(int taskId, IFormFile file, string? note)
        {
            var task = await _context.TaskItems
                .Include(t => t.Submissions)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (task.AssignedUserId != userId)
                return Forbid();

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file.";
                return RedirectToAction("Details", new { id = taskId });
            }

            // Tạo folder
            string folder = Path.Combine("wwwroot", "uploads", "task-submissions", taskId.ToString());
            Directory.CreateDirectory(folder);

            string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + file.FileName;
            string filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // Lưu bản submission
            var submission = new TaskSubmission
            {
                TaskItemId = taskId,
                UserId = userId,
                FilePath = $"/uploads/task-submissions/{taskId}/{fileName}",
                FileName = file.FileName,
                Note = note
            };

            _context.TaskSubmissions.Add(submission);

            // ❗ Cập nhật trạng thái
            if (task.Status == TaskItemStatus.Pending)
                task.Status = TaskItemStatus.InProgress;


            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã nộp task thành công!";
            return RedirectToAction("Details", new { id = taskId });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveTask(int id)
        {
            var task = await _context.TaskItems
        .Include(t => t.Project)
            .ThenInclude(p => p.ProjectMembers)
        .Include(t => t.AssignedUser)
        .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);

            // 🔒 Kiểm tra người duyệt có trong dự án không
            var member = task.Project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == userId);

            // 🔒 Chỉ Owner hoặc Manager được duyệt Task
            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
                return Forbid();

            // ❗ Cập nhật trạng thái
            task.Status = TaskItemStatus.Completed;

            await _context.SaveChangesAsync();
            var assignedUser = task.AssignedUser;
            if (assignedUser != null && !string.IsNullOrEmpty(assignedUser.Email))
            {
                string subject = $"Task '{task.Title}' đã được duyệt";
                string message = $@"
                Xin chào {assignedUser.FullName ?? assignedUser.UserName},

                Task mà bạn được giao đã được duyệt.

                Thông tin chi tiết:
                • Tiêu đề: {task.Title}
                • Dự án: {task.Project.ProjectName}
                • Mô tả: {task.Description}
                • Hạn nộp: {task.DueDate?.ToString("dd/MM/yyyy")}
                • Trạng thái mới: Đã duyệt

                Vui lòng đăng nhập để xem chi tiết.

                Trân trọng!
                ";

                await _emailService.SendEmailAsync(assignedUser.Email, subject, message);
            }

            TempData["SuccessMessage"] = "Đã duyệt task!";
            return RedirectToAction("Details", new { id });
        }
        // GET: TaskItems/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project)
                    .ThenInclude(p => p.ProjectMembers)
                        .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Kiểm tra quyền (Owner hoặc Manager)
            var member = task.Project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == currentUser.Id);

            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
                return Forbid();

            // Load danh sách member
            var members = task.Project.ProjectMembers
                .Select(pm => pm.User!)
                .ToList();

            ViewBag.MemberList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                members.Select(u => new { u.Id, Name = u.FullName ?? u.UserName }),
                "Id",
                "Name",
                task.AssignedUserId
            );

            return View(task);
        }
        // POST: TaskItems/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskItem updatedTask)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project)
                    .ThenInclude(p => p.ProjectMembers)
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == updatedTask.Id);

            if (task == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var member = task.Project.ProjectMembers.FirstOrDefault(pm => pm.UserId == userId);

            // Chỉ Owner/Manager được sửa
            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
                return Forbid();

            // Kiểm tra hạn nộp
            if (updatedTask.DueDate.HasValue && updatedTask.DueDate <= task.CreatedAt)
            {
                TempData["ErrorMessage"] = "Hạn nộp phải sau ngày tạo.";
                return RedirectToAction("Edit", new { id = updatedTask.Id });
            }

            // Kiểm tra thay đổi người được phân công
            bool isAssignedChanged = task.AssignedUserId != updatedTask.AssignedUserId;

            // Cập nhật task
            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.DueDate = updatedTask.DueDate;
            task.AssignedUserId = updatedTask.AssignedUserId;

            await _context.SaveChangesAsync();

            // Nếu thay đổi người được giao → gửi email
            if (isAssignedChanged && updatedTask.AssignedUserId != null)
            {
                var newUser = await _userManager.FindByIdAsync(updatedTask.AssignedUserId);

                if (newUser != null && !string.IsNullOrWhiteSpace(newUser.Email))
                {
                    string subject = $"Bạn vừa được giao task: {task.Title}";
                    string message = $@"
Xin chào {newUser.FullName ?? newUser.UserName},

Bạn vừa được giao một task trong dự án {task.Project.ProjectName}.

• Task: {task.Title}
• Mô tả: {task.Description}
• Hạn nộp: {task.DueDate?.ToString("dd/MM/yyyy")}

Vui lòng đăng nhập để xem chi tiết.

Trân trọng!
";
                    await _emailService.SendEmailAsync(newUser.Email, subject, message);
                }
            }

            TempData["SuccessMessage"] = "Cập nhật task thành công!";
            return RedirectToAction("Details", "Projects", new { id = task.ProjectId });
        }
        // GET: TaskItems/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            // Chỉ Owner & Manager được xoá task
            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
                return Forbid();

            return View(task);
        }

        // POST: TaskItems/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == task.ProjectId && pm.UserId == userId);

            if (member == null || (member.Role != "Owner" && member.Role != "Manager"))
                return Forbid();

            int projectId = task.ProjectId;

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xoá task!";
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
        public async Task<IActionResult> MyTasks()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var tasks = await _context.TaskItems
                .Include(t => t.Project)
                .Where(t => t.AssignedUserId == user.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(tasks);
        }

    }
    public class TaskDetailViewModel
    {
        public TaskItem Task { get; set; }
        public bool IsOwner { get; set; }
        public bool IsManager { get; set; }
        public bool IsAssignedUser { get; set; }
    }
}
