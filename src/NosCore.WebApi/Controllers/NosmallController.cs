using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    public class MallController(IAuthHub authHub, IOptions<MallConfiguration> mallConfiguration) : Controller
    {
        [HttpGet]
        [Route("")]
        public async Task<ActionResult> IndexAsync(string sid, string pid, string user_id, string m_szName, string sas, string c, string shopType,
            string display_language)
        {
            var expectedSignature = MD5.HashData(Encoding.UTF8.GetBytes($"{sid}{pid}{user_id}{m_szName}{mallConfiguration.Value.SignatureKey}"));
            if (!TryFromHex(sas, out var signature) || !CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
            {
                return BadRequest();
            }

            // The client address has already been resolved from X-Forwarded-For by the forwarded-headers
            // middleware, which only honours the configured known proxies.
            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            var expected = await authHub.GetSessionIpAsync(user_id);
            if (remoteIp == null || string.IsNullOrEmpty(expected) || !IPAddress.TryParse(expected, out var expectedIp)
                || !Normalize(remoteIp).Equals(Normalize(expectedIp)))
            {
                return BadRequest();
            }

            var result = Content(@$"<!DOCTYPE html>
<html>
<body>

{WebUtility.HtmlEncode(sid)}
{WebUtility.HtmlEncode(pid)}
{WebUtility.HtmlEncode(user_id)}
{WebUtility.HtmlEncode(m_szName)}
{WebUtility.HtmlEncode(sas)}
{WebUtility.HtmlEncode(c)}
{WebUtility.HtmlEncode(shopType)}
{WebUtility.HtmlEncode(display_language)}

</body>
</html>");
            result.ContentType = "text/html; charset=UTF-8";

            return result;
        }

        private static IPAddress Normalize(IPAddress address) =>
            address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        private static bool TryFromHex(string? hex, out byte[] bytes)
        {
            try
            {
                bytes = Convert.FromHexString(hex ?? string.Empty);
                return true;
            }
            catch (FormatException)
            {
                bytes = [];
                return false;
            }
        }
    }
}
