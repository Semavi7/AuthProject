using AuthProject.Enums;

namespace AuthProject.Entites
{
    public class Verify
    {
        public Guid Id { get; set; }
        public VerifyChannel Channel { get; set; }
        public VerifyType Type { get; set; }
        public Guid UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public VerifyStatus Status { get; set; } = VerifyStatus.Pedding;
        public short AttemptCount { get; set; } = 0;
        public string IpAdress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public User User { get; set; }
    }
}
