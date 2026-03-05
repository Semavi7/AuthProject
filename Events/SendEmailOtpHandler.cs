using AuthProject.Services.EmailService;
using MediatR;

namespace AuthProject.Events
{
    public class SendEmailOtpHandler : INotificationHandler<SendEmailOtpEvent>
    {
        private readonly IEmailService _emailService;

        public SendEmailOtpHandler(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task Handle(SendEmailOtpEvent notification, CancellationToken cancellationToken)
        {
            string subject = "Güvenlik Doğrulama Kodunuz";
            string body = $"<h3>Merhaba,</h3><p>Hesabınızı doğrulamak veya şifrenizi sıfırlamak için onay kodunuz: <b>{notification.Code}</b></p><p>Bu kodu kimseyle paylaşmayın.</p>";

            await _emailService.SendEmailAsync(notification.Email, subject, body);
        }
    }
}
