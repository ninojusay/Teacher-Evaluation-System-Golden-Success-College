using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;
using Teacher_Evaluation_System__Golden_Success_College_.Services;

// NOTE: You must install MailKit and MimeKit NuGet packages.

// Assuming this is placed in the 'Services' folder/namespace.
namespace Teacher_Evaluation_System__Golden_Success_College_.Services
{
    public class EmailService : IEmailService
    {
        // WARNING: Hardcoding credentials here is a major security risk.
        // These should be loaded from secure configuration (Secrets Manager, appsettings.json, etc.)
        private const string SenderEmail = "ninojusay1@gmail.com";
        private const string SenderPassword = "pnps inpq nmyz vwce"; // This is likely an App Password, keep secure!
        private const string SmtpServer = "smtp.gmail.com";
        private const int SmtpPort = 587;

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Golden Success College", SenderEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart(isHtml ? "html" : "plain")
            {
                Text = body
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(SmtpServer, SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                // Note: GMAIL/Google SMTP often requires an App Password, not the user's main password.
                await client.AuthenticateAsync(SenderEmail, SenderPassword);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}