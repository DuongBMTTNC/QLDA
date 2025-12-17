using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLDA.Models;
using QLDA.Services;
using System.Threading.Tasks;

namespace QLDA.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly AppDbContext _context;
        private static readonly Dictionary<string, string> _otpStore = new(); // Lưu OTP tạm thời

        public AccountController(UserManager<ApplicationUser> userManager, EmailService emailService, AppDbContext context)
        {
            _userManager = userManager;
            _emailService = emailService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Sinh mã OTP 6 chữ số
                var otp = new Random().Next(100000, 999999).ToString();
                _otpStore[email] = otp;

                string subject = "🔒 Mã xác nhận tài khoản của bạn";
                string body = $"<p>Xin chào {fullName},</p>" +
                              $"<p>Mã OTP xác nhận của bạn là: <b>{otp}</b></p>" +
                              "<p>Vui lòng nhập mã này vào hệ thống để kích hoạt tài khoản.</p>";

                await _emailService.SendEmailAsync(email, subject, body);

                TempData["Email"] = email;
                return RedirectToAction("VerifyOtp");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["Email"]?.ToString();
            if (email == null)
                return RedirectToAction("Register");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otp)
        {
            if (!_otpStore.ContainsKey(email))
            {
                ModelState.AddModelError("", "Không tìm thấy mã OTP. Vui lòng đăng ký lại.");
                return View();
            }

            if (_otpStore[email] != otp)
            {
                ModelState.AddModelError("", "Mã OTP không đúng.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            _otpStore.Remove(email);
            TempData["SuccessMessage"] = "Xác thực thành công!";
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });
            ViewBag.NotificationCount = await _context.SystemNotificationUsers
    .Where(n => n.ReceiverId == user.Id && !n.IsRead)
    .CountAsync();


            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, IFormFile? avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            user.FullName = fullName;

            // Xử lý upload avatar nếu có
            if (avatar != null && avatar.Length > 0)
            {
                string folder = Path.Combine("wwwroot", "avatars");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = $"{user.Id}_{avatar.FileName}";
                string path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await avatar.CopyToAsync(stream);
                }

                user.AvatarUrl = "/avatars/" + fileName;
            }

            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }
       
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            // Kiểm tra mật khẩu cũ
            var check = await _userManager.CheckPasswordAsync(user, oldPassword);
            if (!check)
            {
                ModelState.AddModelError("", "❌ Mật khẩu cũ không đúng.");
                return View();
            }

            // Lưu tạm mật khẩu mới (chưa đổi ngay)
            TempData["NewPassword"] = newPassword;

            // Sinh OTP
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[user.Email] = otp;

            string subject = "🔒 OTP xác nhận đổi mật khẩu";
            string body = $"<p>Mã OTP đổi mật khẩu của bạn là: <b>{otp}</b></p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            TempData["Email"] = user.Email;

            return RedirectToAction("ConfirmChangePasswordOtp");
        }


        // -------------------------------
        // Bước 2: Nhập OTP đổi mật khẩu
        // -------------------------------
        [HttpGet]
        public IActionResult ConfirmChangePasswordOtp()
        {
            ViewBag.Email = TempData["Email"]?.ToString();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmChangePasswordOtp(string email, string otp)
        {
            if (!_otpStore.ContainsKey(email))
            {
                ModelState.AddModelError("", "Không tìm thấy OTP. Hãy thử lại.");
                return View();
            }

            if (_otpStore[email] != otp)
            {
                ModelState.AddModelError("", "❌ Mã OTP không chính xác.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            string newPass = TempData["NewPassword"]?.ToString();
            if (newPass == null)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra. Hãy thử lại.");
                return View();
            }

            // Reset mật khẩu: xoá mật khẩu cũ rồi đặt mật khẩu mới
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPass);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại.");
                return View();
            }

            _otpStore.Remove(email);

            TempData["SuccessMessage"] = "✔ Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                return View();
            }

            // Sinh OTP
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[email] = otp;

            string subject = "🔒 Mã OTP đặt lại mật khẩu";
            string body = $"<p>Mã OTP đặt lại mật khẩu của bạn là: <b>{otp}</b></p>";

            await _emailService.SendEmailAsync(email, subject, body);

            TempData["ResetEmail"] = email;

            return RedirectToAction("VerifyResetOtp");
        }

        [HttpGet]
        public IActionResult VerifyResetOtp()
        {
            var email = TempData["ResetEmail"]?.ToString();
            if (email == null) return RedirectToAction("ForgotPassword");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult VerifyResetOtp(string email, string otp)
        {
            if (!_otpStore.ContainsKey(email))
            {
                ModelState.AddModelError("", "OTP không hợp lệ hoặc đã hết hạn.");
                return View();
            }

            if (_otpStore[email] != otp)
            {
                ModelState.AddModelError("", "OTP không đúng.");
                return View();
            }

            TempData["ResetEmail"] = email;
            return RedirectToAction("ResetPassword");
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["ResetEmail"]?.ToString();
            if (email == null) return RedirectToAction("ForgotPassword");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("ForgotPassword");

            // Reset mật khẩu theo đúng Identity
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                _otpStore.Remove(email);
                TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công!";
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }
    }
}
