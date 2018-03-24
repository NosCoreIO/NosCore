using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NosCore.Data;
using Microsoft.AspNetCore.Authorization;
using NosCore.DAL;
using NosCore.Core.Encryption;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace NosCore.MasterServer.Controllers
{
    [Route("api/[controller]")]
    public class TokenController : Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> GenerateToken(string UserName, string Password)
        {
            if (ModelState.IsValid)
            {
                AccountDTO account = DAOFactory.AccountDAO.FirstOrDefault(s => s.Name == UserName);


                if (account != null && account.Password.ToLower().Equals(EncryptionHelper.Sha512(Password)))
                {
                    var claims = new[]
                    {
                          new Claim(ClaimTypes.NameIdentifier, UserName),
                          new Claim(ClaimTypes.Role, account.Authority.ToString()),
                    };

                    var token = new JwtSecurityToken(claims: claims);

                    return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
                }
                else
                {
                    return BadRequest("The user name or password is incorrect.");
                }
            }
            return BadRequest("Could not create token");
        }
    }
}