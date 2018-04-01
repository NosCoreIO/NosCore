using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NosCore.Core.Encryption;
using NosCore.Core.Logger;
using NosCore.Data;
using NosCore.DAL;
using NosCore.Domain.Account;

namespace NosCore.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class TokenController : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Post(string UserName, string Password)
        {
            if (ModelState.IsValid)
            {
                AccountDTO account = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == UserName);

                if (account?.Password.ToLower().Equals(EncryptionHelper.Sha512(Password)) == true)
                {
                    var claims = new ClaimsIdentity(new[]
                    {
                          new Claim(ClaimTypes.NameIdentifier, UserName),
                          new Claim(ClaimTypes.Role, account.Authority.ToString()),
                    });
                    var keyByteArray = Encoding.ASCII.GetBytes(EncryptionHelper.Sha512("NosCorePassword"));//TODO replace by configured one
                    var signinKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyByteArray);
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
                else
                {
                    return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
                }
            }
            return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
        }

        [AllowAnonymous]
        [HttpPost("ConnectServer")]
        public IActionResult ConnectServer(string ServerToken)
        {
            if (ModelState.IsValid)
            {
                if (ServerToken == "NosCorePassword")//TODO replace by configured one
                {
                    var claims = new ClaimsIdentity(new[]
                    {
                          new Claim(ClaimTypes.NameIdentifier, "Server"),
                          new Claim(ClaimTypes.Role, nameof(AuthorityType.GameMaster)),
                    });
                    var keyByteArray = Encoding.ASCII.GetBytes(EncryptionHelper.Sha512("NosCorePassword"));//TODO replace by configured one
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
                else
                {
                    return BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_INCORRECT));
                }
            }
            return BadRequest(BadRequest(LogLanguage.Instance.GetMessageFromKey(LanguageKey.AUTH_ERROR)));
        }
    }
}