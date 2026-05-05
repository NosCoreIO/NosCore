using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.WebApi.Controllers
{
    [Route("[controller]")]
    [AllowAnonymous]
    // this file is an entrypoint for Mall creation. it's not recommended to add code here.
    public class MallController(IAuthHub authHub) : Controller
    {
        [HttpGet]
        [Route("")]
        public async Task<ActionResult> IndexAsync(string sid, string pid, string user_id, string m_szName, string sas, string c, string shopType,
            string display_language)
        {
            using var md5 = MD5.Create();
            var md5bytes = md5.ComputeHash(Encoding.Default.GetBytes($"{sid}{pid}{user_id}{m_szName}shtmxpdlfeoqkr"));

            if (sas != Convert.ToHexString(md5bytes))
            {
                throw new ArgumentException(null, nameof(sas));
            }

            var forwarded = Request.Headers["X-Forwarded-For"].ToString();
            var parts = forwarded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var expected = await authHub.GetSessionIpAsync(user_id);
            if (parts.Length == 0 || !IPAddress.TryParse(parts[0], out var headerIp)
                || string.IsNullOrEmpty(expected) || !IPAddress.TryParse(expected, out var expectedIp)
                || !headerIp.Equals(expectedIp))
            {
                throw new ArgumentException(null, "X-Forwarded-For");
            }

            var result = Content(@$"<!DOCTYPE html>
<html>
<body>

{sid}
{pid}
{user_id}
{m_szName}
{sas}
{c}
{shopType}
{display_language}

</body>
</html>");
            result.ContentType = "text/html; charset=UTF-8";

            return result;
        }
    }
}
