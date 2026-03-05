using MediatR;

namespace AuthProject.Events
{
    public record SendEmailOtpEvent
    (
        string Email,
        string Code
    ) : INotification;
}
