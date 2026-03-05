
namespace AuthProject.Services.EmailService
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new System.Net.Mail.SmtpClient(_config["Smtp:Host"])
                {
                    Port = int.Parse(_config["Smtp:Port"]),
                    Credentials = new System.Net.NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
                    EnableSsl = true,
                };

                var mailMessage = new System.Net.Mail.MailMessage
                {
                    From = new System.Net.Mail.MailAddress(_config["Smtp:From"]),
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
