using System.Security.Claims;
using AuthProject.Dtos;
using AuthProject.Enums;
using AuthProject.Services.AuthService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [EndpointSummary("Yeni bir kullanıcı kaydeder")]
        [EndpointDescription("Sisteme e-posta veya telefon numarası ile yeni bir kullanıcı kaydı oluşturur. İşlem sonucunda doğrulama (OTP) kodu gönderilir.")]
        [ProducesResponseType(typeof(RegisterLoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterLoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unkvown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var (message, user) = await _authService.RegisterAsync(dto, ip, userAgent);

            var responseUser = new RegisterLoginResponseDto(user.Id, user.Email, user.PhoneNumber, user.FirstName, user.LastName, user.Status);
            return Ok(new { message, user = responseUser });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EndpointSummary("Kullanıcı girişi yapar")]
        [EndpointDescription("Kullanıcı adı ve şifre ile sisteme giriş yapar. Başarılı girişte HttpOnly Cookie olarak JWT token döner.")]
        [ProducesResponseType(typeof(RegisterLoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] RegisterLoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unkvown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var user = await _authService.ValidateUserAsync(dto);
            var (acccessToken, refreshToken, loggedInUser) = await _authService.LoginAsync(user, ip, userAgent, dto);

            SetTokenCookies(acccessToken, refreshToken);

            var responseUser = new RegisterLoginResponseDto(loggedInUser.Id, loggedInUser.Email, loggedInUser.PhoneNumber, loggedInUser.FirstName, loggedInUser.LastName, loggedInUser.Status);
            return Ok(new { message = "Giriş başarılı.", user = responseUser });
            
        }

        [HttpPost("forget-password")]
        [AllowAnonymous]
        [EndpointSummary("Şifre sıfırlama talebi oluşturur")]
        [EndpointDescription("Kayıtlı e-posta veya telefon numarasına şifre sıfırlama OTP kodu gönderir. Doğrulama adımı için verify_id döner.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.forgetPasswordAsync(dto, ip, userAgent);

            return Ok(new { message = result.message, verify_id = result.verify_id });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [EndpointSummary("Şifre sıfırlar")]
        [EndpointDescription("Şifre sıfırlama OTP kodu ile doğrulama yapılarak kullanıcının şifresi güncellenir.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var message = await _authService.ResetPasswordAsync(dto);
            return Ok(new { message });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EndpointSummary("Erişim tokenını yeniler")]
        [EndpointDescription("HttpOnly Cookie içindeki Refresh Token kullanılarak yeni bir Access Token ve Refresh Token üretir.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh()
        {
            var token = Request.Cookies["RefreshToken"];
            if (string.IsNullOrEmpty(token)) return Unauthorized("Refresh token bulunamadı.");

            var (accessToken, refreshToken) = await _authService.RefreshTokenAsync(token);
            SetTokenCookies(accessToken, refreshToken);

            return Ok(new { message = "Token yenilendi." });
        }

        [HttpPost("logout")]
        [Authorize]
        [EndpointSummary("Kullanıcı çıkış yapar")]
        [EndpointDescription("Aktif oturumu sonlandırır ve Authentication ile RefreshToken Cookie'lerini temizler.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout()
        {
            var sessionIdClaim = User.Claims.FirstOrDefault(c => c.Type == "sessionId")?.Value;
            if (sessionIdClaim != null && Guid.TryParse(sessionIdClaim, out Guid sessionId))
            {
                var message = await _authService.LogoutAsync(sessionId);
                Response.Cookies.Delete("Authentication");
                Response.Cookies.Delete("RefreshToken");
                return Ok(new { message });
            }
            return BadRequest();
        }

        [HttpGet("sessions")]
        [Authorize]
        [EndpointSummary("Aktif oturumları listeler")]
        [EndpointDescription("Giriş yapmış kullanıcının tüm aktif oturumlarını IP adresi, cihaz ve tarih bilgisiyle birlikte döner.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Sessions()
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var sessions = await _authService.GetActiveSessionsAsync(userId);
            return Ok(sessions);
        }

        [HttpPost("verify-account")]
        [AllowAnonymous]
        [EndpointSummary("Kullanıcı hesabını doğrular")]
        [EndpointDescription("Kayıt sonrası gönderilen 6 haneli OTP kodu ile hesabı aktif hale getirir.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountDto dto)
        {
            var message = await _authService.VerifyAccountAsync(dto);
            return Ok(new { message });
        }

        [HttpPost("resend-verification-otp")]
        [AllowAnonymous]
        [EndpointSummary("Doğrulama OTP kodunu yeniden gönderir")]
        [EndpointDescription("Süresi dolmuş veya ulaşmamış OTP kodunu e-posta ya da SMS aracılığıyla tekrar gönderir.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendVerificationOtp([FromBody] ResendOtpDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var message = await _authService.ResendVerificationOtpAsync(dto, ip, userAgent);
            return Ok(new { message });
        }

        [HttpGet("google")]
        [AllowAnonymous]
        [EndpointSummary("Google ile giriş başlatır")]
        [EndpointDescription("Kullanıcıyı Google OAuth 2.0 kimlik doğrulama sayfasına yönlendirir.")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback))
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google/redirect")]
        [AllowAnonymous]
        [EndpointSummary("Google OAuth callback")]
        [EndpointDescription("Google kimlik doğrulaması tamamlandıktan sonra çağrılır. Kullanıcı oluşturulur veya mevcut hesaba bağlanır, JWT token Cookie olarak set edilir ve dashboard'a yönlendirilir.")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GoogleCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync("ExternalCookie");

            if (!authenticateResult.Succeeded)
                return BadRequest(new { message = "Google kimlik doğrulaması başarısız oldu veya kullanıcı reddetti." });

            var claims = authenticateResult.Principal.Claims;
            var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            await HttpContext.SignOutAsync("ExternalCookie");

            if (providerId == null || email == null)
                return BadRequest(new { message = "Google'dan gerekli e-posta bilgisi alınamadı." });

            var profile = new { Id = providerId, Email = email, DisplayName = name ?? "Google User" };
            var user = await _authService.ValidateOAutLoginAsync(profile, SocialiteType.Google);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();
            var dummyLoginDto = new RegisterLoginDto(user.Email, null, "", DeviceType.Web, "Web Browser", Guid.NewGuid().ToString());

            var (accessToken, refreshToken, loggedInUser) = await _authService.LoginAsync(user, ip, userAgent, dummyLoginDto);

            SetTokenCookies(accessToken, refreshToken);

            return Redirect("http://localhost:3000/dashboard");
        }

        private void SetTokenCookies(string accessToken, string refreshToken)
        {
            var isProd = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

            Response.Cookies.Append("Authentication", accessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromMinutes(15)
            });
            Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = isProd,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromDays(7)
            });
        }
    }
}
