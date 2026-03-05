using AuthProject.Entites;
using AuthProject.Enums;

namespace AuthProject.Repositories.AuthRepository
{
    public interface IAuthRepository
    {
        Task<User?> GetUserByPhoneAsync(string phone);

        Task AddUserSessionAsync(UserSession session);
        Task<UserSession?> GetSessionAsync(Guid sessionId, Guid userId);
        Task<UserSession?> GetSessionByIdAsync(Guid sessionId);
        void RemoveSession(UserSession session);
        Task<List<UserSession>> GetActiveSessionsAsync(Guid userId);

        Task AddVerifyAsync(Verify verify);
        Task<Verify?> GetPendingVerifyByIdAsync(Guid verifyId);
        Task<Verify?> GetLatestPendingVerifyAsync(Guid userId, VerifyType type);
        Task<List<Verify>> GetPendingVerifiesAsync(Guid userId, VerifyType type);

        Task AddForgetPasswordAsync(ForgetPassword forgetPassword);
        Task<ForgetPassword?> GetUnusedForgetPasswordAsync(Guid verifyId);

        Task<Socialite?> GetSocialiteAsync(SocialiteType provider, string refId);
        Task AddSocialiteAsync(Socialite socialite);

        Task<int> SaveChangesAsync();
    }
}
