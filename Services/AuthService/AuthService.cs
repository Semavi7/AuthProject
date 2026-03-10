using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using AuthProject.Dtos;
using AuthProject.Entites;
using AuthProject.Enums;
using AuthProject.Events;
using AuthProject.Repositories.AuthRepository;
using AuthProject.Settings;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthProject.Services.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IAuthRepository _authRepository; 
        private readonly IMediator _mediator;
        private readonly JwtSettings _jwtSettings;

        public AuthService(UserManager<User> userManager, IAuthRepository authRepository, IMediator mediator, IOptions<JwtSettings> jwtOptions)
        {
            _userManager = userManager;
            _authRepository = authRepository;
            _mediator = mediator;
            _jwtSettings = jwtOptions.Value;
        }

        public async Task<User> ValidateUserAsync(RegisterLoginDto dto)
        {
            User? user = null;
            if (!string.IsNullOrEmpty(dto.Email))
                user = await _userManager.FindByEmailAsync(dto.Email);
            else if (!string.IsNullOrEmpty(dto.Phone))
                user = await _authRepository.GetUserByPhoneAsync(dto.Phone); 

            if (user == null || !await _userManager.HasPasswordAsync(user))
                throw new UnauthorizedAccessException("Email veya şifre hatalı");

            if (user.Status == UserStatus.Peding)
                throw new UnauthorizedAccessException("Lütfen giriş yapmadan önce hesabınızı doğrulayın.");

            var isMatch = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isMatch)
                throw new UnauthorizedAccessException("Şifre hatalı");

            return user;
        }

        public async Task<(string message, User user)> RegisterAsync(RegisterLoginDto dto, string ip, string userAgent)
        {
            User? existingUser = null;
            if (!string.IsNullOrEmpty(dto.Email)) existingUser = await _userManager.FindByEmailAsync(dto.Email);
            else if (!string.IsNullOrEmpty(dto.Phone)) existingUser = await _authRepository.GetUserByPhoneAsync(dto.Phone); 

            if (existingUser != null) throw new UnauthorizedAccessException("Bu email veya telefon zaten kayıtlı");

            var user = new User
            {
                UserName = string.IsNullOrEmpty(dto.Email) ? dto.Phone : dto.Email,
                Email = string.IsNullOrEmpty(dto.Email) ? null : dto.Email,
                PhoneNumber = string.IsNullOrEmpty(dto.Phone) ? null : dto.Phone,
                Status = UserStatus.Peding,
                CreatedAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Kullanıcı oluşturulamadı: {errors}");
            }

            string otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var channel = !string.IsNullOrEmpty(dto.Email) ? VerifyChannel.Email : VerifyChannel.Sms;

            var verify = new Verify
            {
                Id = Guid.NewGuid(),
                Channel = channel,
                Type = VerifyType.VerifyAccount,
                UserId = user.Id,
                Code = otpCode,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IpAdress = ip ?? "unknown",
                UserAgent = userAgent ?? "unknown",
                Status = VerifyStatus.Pedding
            };

            await _authRepository.AddVerifyAsync(verify); 
            await _authRepository.SaveChangesAsync(); 

            if (channel == VerifyChannel.Email && !string.IsNullOrEmpty(dto.Email))
                await _mediator.Publish(new SendEmailOtpEvent(dto.Email, otpCode));
            else if (channel == VerifyChannel.Sms && !string.IsNullOrEmpty(dto.Phone))
                await _mediator.Publish(new SendSmsOtpEvent(dto.Phone, otpCode));

            return ("Kayıt başarılı. Lütfen gönderilen kod ile hesabınızı doğrulayın.", user);
        }

        public async Task<(string accessToken, string refreshToken, User user)> LoginAsync(User user, string ip, string userAgent, RegisterLoginDto dto)
        {
            var session = new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                DeviceId = dto.DeviceId ?? Guid.NewGuid().ToString(),
                DeviceType = dto.DeviceType ?? DeviceType.Web,
                IpAdress = ip ?? string.Empty,
                UserAgent = userAgent ?? string.Empty,
                LastActiveAt = DateTime.UtcNow,
                ExpireAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
            };

            var accessToken = GenerateAccessToken(user, session.Id);
            var refreshToken = GenerateRefreshToken(user.Id, session.Id);

            session.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            await _authRepository.AddUserSessionAsync(session); 
            await _authRepository.SaveChangesAsync(); 

            return (accessToken, refreshToken, user);
        }

        public async Task<User> ValidateOAutLoginAsync(dynamic profile, SocialiteType provider)
        {
            string id = profile.Id;
            string email = profile.Email;
            string displayName = profile.DisplayName;

            var socialite = await _authRepository.GetSocialiteAsync(provider, id); 

            if (socialite != null) return socialite.User;

            User? user = !string.IsNullOrEmpty(email) ? await _userManager.FindByEmailAsync(email) : null;

            if (user == null)
            {
                user = new User
                {
                    UserName = email ?? $"{id}@{provider}.local",
                    Email = email ?? $"{id}@{provider}.local",
                    FirstName = displayName?.Split(' ')[0] ?? id,
                    LastName = displayName?.Contains(" ") == true ? displayName.Substring(displayName.IndexOf(' ') + 1) : "",
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                };
                await _userManager.CreateAsync(user);
            }

            var newSocialite = new Socialite
            {
                Id = Guid.NewGuid(),
                Type = provider,
                RefId = id,
                Email = email,
                UserId = user.Id,
                Data = "{}"
            };

            await _authRepository.AddSocialiteAsync(newSocialite); 
            await _authRepository.SaveChangesAsync(); 

            return user;
        }

        public async Task<(string message, Guid verify_id)> forgetPasswordAsync(ForgetPasswordRequestDto dto, string ip, string userAgent)
        {
            User? user = null;
            if (!string.IsNullOrEmpty(dto.Email)) user = await _userManager.FindByEmailAsync(dto.Email);
            else if (!string.IsNullOrEmpty(dto.Phone)) user = await _authRepository.GetUserByPhoneAsync(dto.Phone); 

            if (user == null) throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            string otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            DateTime expireDate = DateTime.UtcNow.AddMinutes(15);
            var channel = !string.IsNullOrEmpty(dto.Email) ? VerifyChannel.Email : VerifyChannel.Sms;

            var verify = new Verify
            {
                Id = Guid.NewGuid(),
                Channel = channel,
                Type = VerifyType.ForgetPassword,
                UserId = user.Id,
                Code = otpCode,
                ExpiredAt = expireDate,
                IpAdress = ip ?? "unknown",
                UserAgent = userAgent ?? "unknown"
            };

            var forgetPassword = new ForgetPassword
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                VerifyId = verify.Id,
                ExpireAt = expireDate
            };

            await _authRepository.AddVerifyAsync(verify); 
            await _authRepository.AddForgetPasswordAsync(forgetPassword); 
            await _authRepository.SaveChangesAsync(); 

            if (channel == VerifyChannel.Email && !string.IsNullOrEmpty(dto.Email)) await _mediator.Publish(new SendEmailOtpEvent(dto.Email, otpCode));
            else if (channel == VerifyChannel.Sms && !string.IsNullOrEmpty(dto.Phone)) await _mediator.Publish(new SendSmsOtpEvent(dto.Phone, otpCode));

            return ("Şifre sıfırlama kodu gönderildi. Lütfen kodu kullanarak şifrenizi sıfırlayın.", verify.Id);
        }

        public async Task<string> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var verify = await _authRepository.GetPendingVerifyByIdAsync(dto.VerifyId); 

            if (verify == null || verify.ExpiredAt < DateTime.UtcNow || verify.AttemptCount >= 5)
                throw new ArgumentException("Geçersiz doğrulama işlemi.");

            if (verify.Code != dto.Code)
            {
                verify.AttemptCount += 1;
                await _authRepository.SaveChangesAsync(); 
                throw new ArgumentException("Doğrulama kodu hatalı.");
            }

            var forgetRecord = await _authRepository.GetUnusedForgetPasswordAsync(dto.VerifyId); 

            if (forgetRecord == null) throw new ArgumentException("Kayıt bulunamadı.");

            var user = await _userManager.FindByIdAsync(forgetRecord.UserId.ToString());
            if (user == null) throw new KeyNotFoundException("Kullanıcı bulunamadı");

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, dto.NewPassword);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                throw new Exception($"Şifre sıfırlanamadı: {errors}");
            }

            verify.Status = VerifyStatus.Complated;
            forgetRecord.IsUsedAt = DateTime.UtcNow;

            await _authRepository.SaveChangesAsync(); 

            return "Şifreniz başarıyla sıfırlandı. Artık yeni şifrenizle giriş yapabilirsiniz.";
        }

        public async Task<string> VerifyAccountAsync(VerifyAccountDto dto)
        {
            User? user = null;
            if (!string.IsNullOrEmpty(dto.Email))
                user = await _userManager.FindByEmailAsync(dto.Email);
            else if (!string.IsNullOrEmpty(dto.Phone))
                user = await _authRepository.GetUserByPhoneAsync(dto.Phone); 

            if (user == null) throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            if (user.Status == UserStatus.Active) throw new UnauthorizedAccessException("Hesabınız zaten doğrulanmış.");

            var verify = await _authRepository.GetLatestPendingVerifyAsync(user.Id, VerifyType.VerifyAccount); 

            if (verify!.AttemptCount >= 5)
            {
                verify.Status = VerifyStatus.Complated;
                await _authRepository.SaveChangesAsync(); 
                throw new ArgumentException("Çok fazla hatalı deneme. Lütfen yeni bir kod isteyin.");
            }

            if (verify.Code != dto.Code)
            {
                verify.AttemptCount += 1;
                await _authRepository.SaveChangesAsync(); 
                throw new ArgumentException("Doğrulama kodu hatalı. Lütfen tekrar deneyin.");
            }

            verify.Status = VerifyStatus.Complated;
            user.Status = UserStatus.Active;

            if (verify.Channel == VerifyChannel.Email) user.EmailVerifyId = verify.Id;
            else user.PhoneVerifyId = verify.Id;

            await _userManager.UpdateAsync(user);
            await _authRepository.SaveChangesAsync(); 

            return "Hesabınız başarıyla doğrulandı. Artık giriş yapabilirsiniz.";
        }

        public async Task<string> ResendVerificationOtpAsync(ResendOtpDto dto, string ip, string userAgent)
        {
            User? user = null;
            if (!string.IsNullOrEmpty(dto.Email))
                user = await _userManager.FindByEmailAsync(dto.Email);
            else if (!string.IsNullOrEmpty(dto.Phone))
                user = await _authRepository.GetUserByPhoneAsync(dto.Phone); 

            if (user == null) throw new KeyNotFoundException("Kullanıcı bulunamadı.");
            if (user.Status == UserStatus.Active) throw new ArgumentException("Hesap zaten doğrulanmış.");

            var pendingVerifies = await _authRepository.GetPendingVerifiesAsync(user.Id, VerifyType.VerifyAccount); 

            foreach (var pv in pendingVerifies) pv.Status = VerifyStatus.Complated;

            string otpCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var channel = !string.IsNullOrEmpty(dto.Email) ? VerifyChannel.Email : VerifyChannel.Sms;

            var verify = new Verify
            {
                Id = Guid.NewGuid(),
                Channel = channel,
                Type = VerifyType.VerifyAccount,
                UserId = user.Id,
                Code = otpCode,
                ExpiredAt = DateTime.UtcNow.AddMinutes(5),
                IpAdress = ip ?? "unknown",
                UserAgent = userAgent ?? "unknown"
            };

            await _authRepository.AddVerifyAsync(verify); 
            await _authRepository.SaveChangesAsync(); 

            if (channel == VerifyChannel.Email && !string.IsNullOrEmpty(dto.Email))
                await _mediator.Publish(new SendEmailOtpEvent(dto.Email, otpCode));
            else if (channel == VerifyChannel.Sms && !string.IsNullOrEmpty(dto.Phone))
                await _mediator.Publish(new SendSmsOtpEvent(dto.Phone, otpCode));

            return "Yeni doğrulama kodu gönderildi.";
        }

        public async Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap.Clear();

            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret!))
            };

            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(token, validationParams, out _);
            }
            catch
            {
                throw new UnauthorizedAccessException("Geçersiz refresh token.");
            }

            var sessionIdStr = principal.Claims.First(c => c.Type == "sessionId").Value;
            var userIdStr = principal.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;

            var sessionId = Guid.Parse(sessionIdStr);
            var userId = Guid.Parse(userIdStr);

            var session = await _authRepository.GetSessionAsync(sessionId, userId); 

            if (session == null) throw new UnauthorizedAccessException("Oturum bulunamadı.");
            if (session.RevokeAt != null) throw new UnauthorizedAccessException("Oturum iptal edilmiş");
            if (session.ExpireAt < DateTime.UtcNow)
            {
                _authRepository.RemoveSession(session); 
                await _authRepository.SaveChangesAsync(); 
                throw new UnauthorizedAccessException("Oturum süresi dolmuş.");
            }

            var isValid = BCrypt.Net.BCrypt.Verify(token, session.RefreshTokenHash);
            if (!isValid) throw new UnauthorizedAccessException("Geçersiz refresh token.");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new KeyNotFoundException("Kullanıcı bulunamadı.");

            var newAccessToken = GenerateAccessToken(user, session.Id);
            var newRefreshToken = GenerateRefreshToken(user.Id, session.Id);

            session.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            session.LastActiveAt = DateTime.UtcNow;

            await _authRepository.SaveChangesAsync(); 

            return (newAccessToken, newRefreshToken);
        }

        public async Task<string> LogoutAsync(Guid sessionId)
        {
            var session = await _authRepository.GetSessionByIdAsync(sessionId); 
            if (session == null) throw new KeyNotFoundException("Oturum bulunamadı.");

            session.RevokeAt = DateTime.UtcNow;
            await _authRepository.SaveChangesAsync(); 

            return "Başarıyla çıkış yapıldı.";
        }

        public async Task<List<UserSession>> GetActiveSessionsAsync(Guid userId)
        {
            var sessions = await _authRepository.GetActiveSessionsAsync(userId); 

            return sessions.Select(s => new UserSession
            {
                Id = s.Id,
                DeviceId = s.DeviceId,
                DeviceName = s.DeviceName,
                LastActiveAt = s.LastActiveAt,
                ExpireAt = s.ExpireAt,
                IpAdress = s.IpAdress,
                CreatedAt = s.CreatedAt
            }).ToList();
        }

        private string GenerateAccessToken(User user, Guid sessionId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim("phone", user.PhoneNumber ?? ""),
                new Claim("sessionId", sessionId.ToString())
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken(Guid userId, Guid sessionId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("sessionId", sessionId.ToString())
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtSettings.Secret!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
