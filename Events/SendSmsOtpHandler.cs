using AuthProject.Services.SmsSevice;
using MediatR;

namespace AuthProject.Events
{
    public class SendSmsOtpHandler : INotificationHandler<SendSmsOtpEvent>
    {
        private readonly ISmsService _smsService;

        public SendSmsOtpHandler(ISmsService smsService)
        {
            _smsService = smsService;
        }

        public async Task Handle(SendSmsOtpEvent notification, CancellationToken cancellationToken)
        {
            string text = $"Sistem dogrulama kodunuz: {notification.Code}. Lutfen kimseyle paylasmayiniz.";

            await _smsService.SendSmsAsync(notification.PhoneNumber, text);
        }
    }
}
