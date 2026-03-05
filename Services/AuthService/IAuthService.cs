using AuthProject.Dtos;
using AuthProject.Entites;
using AuthProject.Enums;

namespace AuthProject.Services.AuthService
{
    public interface IAuthService
    {
        Task<User> ValidateUserAsync(RegisterLoginDto dto);
        Task<(string message, User user)> RegisterAsync(RegisterLoginDto dto, string ip, string userAgent);
        Task<(string accessToken, string refreshToken, User user)> LoginAsync(User user, string ip, string userAgent, RegisterLoginDto dto);
        Task<User> ValidateOAutLoginAsync(dynamic profile, SocialiteType provider);
        Task<(string message, Guid verify_id)> forgetPasswordAsync(ForgetPasswordRequestDto dto, string ip, string userAgent);
        Task<string> ResetPasswordAsync(ResetPasswordDto dto);
        Task<string> VerifyAccountAsync(VerifyAccountDto dto);
        Task<string> ResendVerificationOtpAsync(ResendOtpDto dto, string ip, string userAgent);
        Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string token);
        Task<string> LogoutAsync(Guid sessionId);
        Task<List<UserSession>> GetActiveSessionsAsync(Guid userId);
    }
}
