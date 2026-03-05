using AuthProject.Enums;

namespace AuthProject.Entites
{
    public class UserSession
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string RefreshTokenHash { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; } = DeviceType.Web;
        public string DeviceName { get; set; } = string.Empty;
        public string IpAdress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTime LastActiveAt { get; set; }
        public DateTime ExpireAt { get; set; }
        public DateTime? RevokeAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; }
    }
}
