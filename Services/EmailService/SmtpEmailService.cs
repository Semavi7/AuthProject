
using AuthProject.Settings;
using Microsoft.Extensions.Options;

namespace AuthProject.Services.EmailService
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public SmtpEmailService(IOptions<SmtpSettings> options)
        {
            _smtpSettings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new System.Net.Mail.SmtpClient(_smtpSettings.Host)
                {
                    Port = _smtpSettings.Port,
                    Credentials = new System.Net.NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = true,
                };

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_smtpSettings.From),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mail gönderim hatası: {ex.Message}");
            }
        }
    }
}
