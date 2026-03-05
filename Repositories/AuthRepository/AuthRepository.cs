using AuthProject.Db;
using AuthProject.Entites;
using AuthProject.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuthProject.Repositories.AuthRepository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<User?> GetUserByPhoneAsync(string phone)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }

        public async Task AddUserSessionAsync(UserSession session)
        {
            await _dbContext.UserSessions.AddAsync(session);
        }

        public async Task<UserSession?> GetSessionAsync(Guid sessionId, Guid userId)
        {
            return await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }

        public async Task<UserSession?> GetSessionByIdAsync(Guid sessionId)
        {
            return await _dbContext.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public void RemoveSession(UserSession session)
        {
            _dbContext.UserSessions.Remove(session);
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(Guid userId)
        {
            return await _dbContext.UserSessions
                .Where(s => s.UserId == userId && s.RevokeAt == null && s.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(s => s.LastActiveAt)
                .ToListAsync();
        }

        public async Task AddVerifyAsync(Verify verify)
        {
            await _dbContext.Verifies.AddAsync(verify);
        }

        public async Task<Verify?> GetPendingVerifyByIdAsync(Guid verifyId)
        {
            return await _dbContext.Verifies.FirstOrDefaultAsync(v => v.Id == verifyId && v.Status == VerifyStatus.Pedding);
        }

        public async Task<Verify?> GetLatestPendingVerifyAsync(Guid userId, VerifyType type)
        {
            return await _dbContext.Verifies
                .Where(v => v.UserId == userId && v.Type == type && v.Status == VerifyStatus.Pedding)
                .OrderByDescending(v => v.ExpiredAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Verify>> GetPendingVerifiesAsync(Guid userId, VerifyType type)
        {
            return await _dbContext.Verifies
                .Where(v => v.UserId == userId && v.Type == type && v.Status == VerifyStatus.Pedding)
                .ToListAsync();
        }

        public async Task AddForgetPasswordAsync(ForgetPassword forgetPassword)
        {
            await _dbContext.ForgetPasswords.AddAsync(forgetPassword);
        }

        public async Task<ForgetPassword?> GetUnusedForgetPasswordAsync(Guid verifyId)
        {
            return await _dbContext.ForgetPasswords.FirstOrDefaultAsync(f => f.VerifyId == verifyId && f.IsUsedAt == null);
        }

        public async Task<Socialite?> GetSocialiteAsync(SocialiteType provider, string refId)
        {
            return await _dbContext.Socialites.Include(s => s.User).FirstOrDefaultAsync(s => s.Type == provider && s.RefId == refId);
        }

        public async Task AddSocialiteAsync(Socialite socialite)
        {
            await _dbContext.Socialites.AddAsync(socialite);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
