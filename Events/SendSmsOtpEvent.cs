using MediatR;

namespace AuthProject.Events
{
    public record SendSmsOtpEvent
    (
        string PhoneNumber,
        string Code
    ) : INotification;
}
