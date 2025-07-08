using System.Threading.Tasks;

namespace HR_Products.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string recipientEmail, string subject, string message, string fromEmail, string fromName);
    }
}