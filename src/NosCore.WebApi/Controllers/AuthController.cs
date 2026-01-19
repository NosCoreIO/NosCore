//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations.I18N;
using NosCore.Data.WebApi;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.GameObject.Services.AuthService;
using NosCore.Shared.Authentication;
using NosCore.Shared.Configuration;
using NosCore.Shared.Enumerations;
using NosCore.Shared.I18N;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TwoFactorAuthNet;

namespace NosCore.WebApi.Controllers
{
    [ApiController]
    [Route("api/v1/auth/thin")]
    [Route("api/[controller]")]
    public class AuthController(IOptions<WebApiConfiguration> apiConfiguration, IDao<AccountDto, long> accountDao,
            ILogger<AuthController> logger, IHasher hasher, ILogLanguageLocalizer<LogLanguageKey> logLanguage,
            IAuthHub authHub, IAuthCodeService authCodeService)
        : Controller
    {
        [AllowAnonymous]
        [HttpPost("sessions")]
        public async Task<IActionResult> ConnectUserAsync(ApiSession session)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(logLanguage[LogLanguageKey.AUTH_ERROR]);
            }

            var account = await accountDao.FirstOrDefaultAsync(s => s.Name == session.Identity);
            if (account == null)
            {
                return BadRequest(logLanguage[LogLanguageKey.AUTH_ERROR]);
            }
            var tfa = new TwoFactorAuth();
            if (!string.IsNullOrEmpty(account.MfaSecret) && !tfa.VerifyCode(account.MfaSecret, session.Mfa))
            {
                return BadRequest(logLanguage[LogLanguageKey.MFA_INCORRECT]);
            }

            if (account.Password?.ToLower(CultureInfo.CurrentCulture) != (hasher.Hash(session.Password))
                && account.NewAuthPassword?.ToLower(CultureInfo.CurrentCulture) != (hasher.Hash(session.Password, account.NewAuthSalt!)))
            {
                return BadRequest(logLanguage[LogLanguageKey.AUTH_INCORRECT]);
            }

            account.Language = Enum.Parse<RegionType>(session.GfLang?.ToUpper(CultureInfo.CurrentCulture) ?? "");

            account = await accountDao.TryInsertOrUpdateAsync(account);
            var platformGameAccountId = Guid.NewGuid();
            var claims = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.Identity),
                new Claim(ClaimTypes.Sid, platformGameAccountId.ToString()),
                new Claim(ClaimTypes.Role, account.Authority.ToString())
            });
            var password = hasher.Hash(apiConfiguration.Value.Password!, apiConfiguration.Value.Salt);

            var keyByteArray = Encoding.Default.GetBytes(password);
            var signinKey = new SymmetricSecurityKey(keyByteArray);
            var handler = new JwtSecurityTokenHandler();
            var securityToken = handler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = claims,
                Issuer = "Issuer",
                Audience = "Audience",
                SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256Signature)
            });
            logger.LogInformation(logLanguage[LogLanguageKey.AUTH_API_SUCCESS],
                session.Identity, platformGameAccountId, session.Locale);
            return Ok(new
            {
                token = handler.WriteToken(securityToken),
                platformGameAccountId
            });
        }

        [HttpPost("codes")]
        public IActionResult GetAuthCode(ApiPlatformGameAccount platformGameAccount)
        {
            var identity = (ClaimsIdentity?)User.Identity;
            if (identity?.Claims.Any(s =>
                (s.Type == ClaimTypes.Sid) && (s.Value == platformGameAccount.PlatformGameAccountId)) != true)
            {
                return BadRequest(logLanguage[LogLanguageKey.AUTH_INCORRECT]);
            }

            var authCode = Guid.NewGuid();
            authCodeService.StoreAuthCode(authCode.ToString(),
                identity.Claims.First(s => s.Type == ClaimTypes.NameIdentifier).Value);

            return Ok(new { code = authCode });
        }


        [HttpPost]
        public IActionResult SetExpectingConnection([FromBody] AuthIntent intent)
        {
            if (intent == null!)
            {
                return BadRequest(logLanguage[LogLanguageKey.AUTH_INCORRECT]);
            }

            authHub.SetAwaitingConnectionAsync(intent.SessionId, intent.AccountName);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetExpectingConnection(string? id, string? token, int sessionId)
        {
            return Ok(await authHub.GetAwaitingConnectionAsync(id, token, sessionId));
        }

        [HttpGet("MfaEnabled")]
        [AllowAnonymous]
        public async Task<IActionResult> HasMfaEnabled(string? username)
        {
            var account = await accountDao.FirstOrDefaultAsync(s => s.Name == username);
            if (account == null || account.MfaSecret == null)
            {
                return Ok(false);
            }

            return Ok(true);
        }

    }

    [Serializable]
    public class ApiSession
    {
        [Required]
        public string GfLang { get; set; } = null!;
        [Required]
        public string Identity { get; set; } = null!;
        [Required]
        public string Locale { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        public string? Mfa { get; set; }
    }

    [Serializable]
    public class ApiPlatformGameAccount
    {
        public string? PlatformGameAccountId { get; set; }
    }
}
