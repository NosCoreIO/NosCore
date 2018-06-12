using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer.Controllers
{
	[Route("api/[controller]")]
	public class TokenController : Controller
	{
		private readonly WebApiConfiguration _apiConfiguration;

		public TokenController(WebApiConfiguration apiConfiguration)
		{
			_apiConfiguration = apiConfiguration;
		}

		[AllowAnonymous]
		[HttpPost]
		public IActionResult Post(string UserName, string Password)
		{
			if (ModelState.IsValid)
			{
				var account = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == UserName);

				if (account?.Password.ToLower().Equals(EncryptionHelper.Sha512(Password)) == true)
				{
					var claims = new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.NameIdentifier, UserName),
						new Claim(ClaimTypes.Role, account.Authority.ToString())
					});
					var keyByteArray = Encoding.Default.GetBytes(EncryptionHelper.Sha512(_apiConfiguration.Password));
					var signinKey = new SymmetricSecurityKey(keyByteArray);
					var handler = new JwtSecurityTokenHandler();
					var securityToken = handler.CreateToken(new SecurityTokenDescriptor
					{
						Subject = claims,
						Issuer = "Issuer",
						Audience = "Audience",
						SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
					});
					return Ok(handler.WriteToken(securityToken));
				}

				return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
			}

			return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
		}

		[AllowAnonymous]
		[HttpPost("ConnectServer")]
		public IActionResult ConnectServer(string ServerToken)
		{
			if (ModelState.IsValid)
			{
				if (ServerToken == _apiConfiguration.Password)
				{
					var claims = new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.NameIdentifier, "Server"),
						new Claim(ClaimTypes.Role, nameof(AuthorityType.GameMaster))
					});
					var keyByteArray = Encoding.Default.GetBytes(EncryptionHelper.Sha512(_apiConfiguration.Password));
					var signinKey = new SymmetricSecurityKey(keyByteArray);
					var handler = new JwtSecurityTokenHandler();
					var securityToken = handler.CreateToken(new SecurityTokenDescriptor
					{
						Subject = claims,
						Issuer = "Issuer",
						Audience = "Audience",
						SigningCredentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
					});
					return Ok(handler.WriteToken(securityToken));
				}

				return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
			}

			return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
		}
	}
}