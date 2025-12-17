using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

namespace QLDA.Services
{
    public class EmailService
    {
        private readonly string _fromEmail = "tranquocduongg2001@gmail.com"; // Gmail của bạn
        private readonly string _fromName = "Hệ thống QLDA";
        private readonly string _appPassword = "unxy liij ante rrcp"; // Mật khẩu ứng dụng Gmail

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_fromEmail, _appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}

