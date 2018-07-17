using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NosCore.Configuration;
using NosCore.Core.Encryption;
using NosCore.Data.WebApi;
using NosCore.DAL;
using NosCore.Shared.Enumerations.Account;
using NosCore.Shared.I18N;

namespace NosCore.WorldServer.Controllers
{
	[Route("api/[controller]")]
	[AllowAnonymous]
    public class TokenController : Controller
	{
		private readonly WebApiConfiguration _apiConfiguration;

		public TokenController(WebApiConfiguration apiConfiguration)
		{
			_apiConfiguration = apiConfiguration;
		}

		[HttpPost]
		public IActionResult ConnectUser([FromBody]WebApiClient client)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
			}

			var account = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == client.Username);

			if (account?.Password.ToLower().Equals(EncryptionHelper.Sha512(client.Password)) != true)
			{
				return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
			}

			var claims = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, client.Username),
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

		[HttpPost("ConnectServer")]
		public IActionResult ConnectServer([FromBody]WebApiToken serverWebApiToken)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
			}

			if (serverWebApiToken.ServerToken != _apiConfiguration.Password)
			{
				return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
			}

			var claims = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, "Server"),
				new Claim(ClaimTypes.Role, nameof(AuthorityType.Root))
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
	}
}