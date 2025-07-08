using HR_Products.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HR_Products.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        public async Task SendEmailAsync(string recipientEmail, string subject, string message, string replyToEmail, string fromDisplayName)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.AuthUsername, fromDisplayName ?? "HR System"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(recipientEmail);

            if (!string.IsNullOrEmpty(replyToEmail))
            {
                mailMessage.ReplyToList.Add(new MailAddress(replyToEmail));
            }

            using (var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(
                    _emailSettings.AuthUsername,
                    _emailSettings.AuthPassword);
                smtpClient.EnableSsl = _emailSettings.EnableSsl;
                smtpClient.Timeout = 10000; // 10 seconds

                await smtpClient.SendMailAsync(mailMessage);
            }
        }
    }
}
