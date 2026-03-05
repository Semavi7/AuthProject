using AuthProject.Enums;

namespace AuthProject.Entites
{
    public class Socialite
    {
        public Guid Id { get; set; }
        public SocialiteType Type { get; set; } = SocialiteType.Google;
        public string Data { get; set; } = string.Empty;
        public string RefId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}
