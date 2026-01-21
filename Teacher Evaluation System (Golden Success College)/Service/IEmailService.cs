using System.Threading.Tasks;

namespace Teacher_Evaluation_System__Golden_Success_College_.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }
}