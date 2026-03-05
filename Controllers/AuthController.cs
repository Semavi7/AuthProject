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
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var result = await _authService.forgetPasswordAsync(dto, ip, userAgent);

            return Ok(new { message = result.message, verify_id = result.verify_id });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var message = await _authService.ResetPasswordAsync(dto);
            return Ok(new { message });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
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
        public async Task<IActionResult> Sessions()
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var sessions = await _authService.GetActiveSessionsAsync(userId);
            return Ok(sessions);
        }

        [HttpPost("verify-account")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyAccount([FromBody] VerifyAccountDto dto)
        {
            var message = await _authService.VerifyAccountAsync(dto);
            return Ok(new { message });
        }

        [HttpPost("resend-verification-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendVerificationOtp([FromBody] ResendOtpDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();

            var message = await _authService.ResendVerificationOtpAsync(dto, ip, userAgent);
            return Ok(new { message });
        }

        [HttpGet("google")]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            // Kullanıcıyı Google giriş sayfasına yönlendir (NestJS: @UseGuards(GoogleAuthGuard) mantığı)
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(GoogleCallback)) // Google'ın döneceği adres
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google/redirect")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback()
        {
            // 1. Google'dan dönen verileri aracı "ExternalCookie" üzerinden okuyoruz
            var authenticateResult = await HttpContext.AuthenticateAsync("ExternalCookie");

            if (!authenticateResult.Succeeded)
                return BadRequest(new { message = "Google kimlik doğrulaması başarısız oldu veya kullanıcı reddetti." });

            // 2. Kullanıcı bilgilerini (Profile) çıkarıyoruz
            var claims = authenticateResult.Principal.Claims;
            var providerId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            // 3. İşimiz bittiği için o geçici çerezi (ExternalCookie) hemen siliyoruz
            await HttpContext.SignOutAsync("ExternalCookie");

            if (providerId == null || email == null)
                return BadRequest(new { message = "Google'dan gerekli e-posta bilgisi alınamadı." });

            // 4. AuthService'deki yazdığımız metodu çağırıp kullanıcıyı buluyoruz/yaratıyoruz
            var profile = new { Id = providerId, Email = email, DisplayName = name ?? "Google User" };
            var user = await _authService.ValidateOAutLoginAsync(profile, SocialiteType.Google);

            // 5. Cihaz bilgilerini varsayılan olarak ayarlayıp sisteme Login yapıyoruz
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = Request.Headers["User-Agent"].ToString();
            var dummyLoginDto = new RegisterLoginDto(user.Email, null, "", DeviceType.Web, "Web Browser", Guid.NewGuid().ToString());

            var (accessToken, refreshToken, loggedInUser) = await _authService.LoginAsync(user, ip, userAgent, dummyLoginDto);

            // 6. Güvenli HttpOnly Cookie'lere bizim sistemimizin tokenlarını atıyoruz
            SetTokenCookies(accessToken, refreshToken);

            // 7. SON ADIM: Frontend'in adresine yönlendiriyoruz!
            // Çerezler tarayıcıya yerleştiği için, kullanıcı React/Angular anasayfasına düştüğünde giriş yapmış olacak.
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
