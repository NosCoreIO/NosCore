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

namespace NosCore.MasterServer.Controllers
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
		public IActionResult Post(string userName, string password)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
			}

			var account = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == userName);

			if (account?.Password.ToLower().Equals(EncryptionHelper.Sha512(password)) != true)
			{
				return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
			}

			var claims = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, userName),
				new Claim(ClaimTypes.Role, account.Authority.ToString())
			});
			var keyByteArray = Encoding.ASCII.GetBytes(EncryptionHelper.Sha512(_apiConfiguration.Password));
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

		[AllowAnonymous]
		[HttpPost("ConnectServer")]
		public IActionResult ConnectServer(string serverToken)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
			}

			if (serverToken != _apiConfiguration.Password)
			{
				return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
			}

			var claims = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, "Server"),
				new Claim(ClaimTypes.Role, nameof(AuthorityType.GameMaster))
			});
			var keyByteArray = Encoding.ASCII.GetBytes(EncryptionHelper.Sha512(_apiConfiguration.Password));
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
	}
}