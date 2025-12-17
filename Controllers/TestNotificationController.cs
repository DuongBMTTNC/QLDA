using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QLDA.Models;
using QLDA.Services;

namespace QLDA.Controllers
{
    public class TestNotificationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public TestNotificationController(UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        public async Task<IActionResult> SendEmailTest()
        {
            // Lấy user có email cần test
            var user = await _userManager.FindByEmailAsync("quocduongtran19@gmail.com");

            if (user == null)
                return Content("❌ Không tìm thấy người dùng với email này trong database.");

            // Gửi mail
            string subject = "📢 Thông báo thử nghiệm từ hệ thống QLDA";
            string body = $@"
                <h3>Xin chào {user.FullName ?? user.UserName},</h3>
                <p>Đây là email thử nghiệm được gửi từ hệ thống QLDA.</p>
                <p>Thời gian gửi: {DateTime.Now:HH:mm:ss dd/MM/yyyy}</p>
                <p><i>Nếu bạn nhận được email này, nghĩa là tính năng gửi thông báo hoạt động tốt.</i></p>
            ";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return Content($"✅ Email thông báo thử đã được gửi đến {user.Email}.");
        }
    }
}
