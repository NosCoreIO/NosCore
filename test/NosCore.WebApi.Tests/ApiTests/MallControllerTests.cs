//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.WebApi.Controllers;
using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.WebApi.Tests.ApiTests
{
    [TestClass]
    public class MallControllerTests
    {
        private const string SignatureKey = "a-configured-mall-key";
        private const string Account = "account";
        private static readonly IPAddress SessionIp = IPAddress.Parse("203.0.113.7");

        private static MallController Controller(IPAddress remoteIp, string signatureKey = SignatureKey)
        {
            var authHub = new Mock<IAuthHub>();
            authHub.Setup(s => s.GetSessionIpAsync(Account)).ReturnsAsync(SessionIp.ToString());
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = remoteIp;
            return new MallController(authHub.Object, Options.Create(new MallConfiguration { SignatureKey = signatureKey }))
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        private static string Sign(string sid, string pid, string userId, string name, string key = SignatureKey) =>
            Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes($"{sid}{pid}{userId}{name}{key}")));

        [TestMethod]
        public async Task EchoedValuesAreHtmlEncoded()
        {
            var result = await Controller(SessionIp).IndexAsync("1", "2", Account, "name", Sign("1", "2", Account, "name"),
                "<script>c</script>", "\"shop\"", "<b>en</b>");

            var content = ((ContentResult)result).Content!;
            Assert.IsFalse(content.Contains("<script>", StringComparison.Ordinal));
            Assert.IsFalse(content.Contains("<b>", StringComparison.Ordinal));
            StringAssert.Contains(content, "&lt;script&gt;c&lt;/script&gt;");
            StringAssert.Contains(content, "&quot;shop&quot;");
        }

        [TestMethod]
        public async Task SignatureUsesTheConfiguredKey()
        {
            var signedWithAnotherKey = Sign("1", "2", Account, "name", "another-key");

            var result = await Controller(SessionIp).IndexAsync("1", "2", Account, "name", signedWithAnotherKey, "c", "shop", "en");

            Assert.IsInstanceOfType<BadRequestResult>(result);
        }

        [TestMethod]
        public async Task RequestFromAnotherAddressThanTheSessionIsRefused()
        {
            var result = await Controller(IPAddress.Parse("198.51.100.1"))
                .IndexAsync("1", "2", Account, "name", Sign("1", "2", Account, "name"), "c", "shop", "en");

            Assert.IsInstanceOfType<BadRequestResult>(result);
        }

        private static async Task<IPAddress?> ResolvedClientIp(IPAddress remoteIp, string forwardedFor, ForwardedHeadersConfiguration configuration)
        {
            var options = new ForwardedHeadersOptions();
            WebApiBootstrap.ConfigureForwardedHeaders(options, configuration);
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = remoteIp;
            context.Request.Headers["X-Forwarded-For"] = forwardedFor;
            await new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options)).Invoke(context);
            return context.Connection.RemoteIpAddress;
        }

        [TestMethod]
        public async Task ForwardedForIsIgnoredFromAnUnknownSender()
        {
            var sender = IPAddress.Parse("198.51.100.1");

            var resolved = await ResolvedClientIp(sender, SessionIp.ToString(), new ForwardedHeadersConfiguration());

            Assert.AreEqual(sender, resolved);
        }

        [TestMethod]
        public async Task ForwardedForIsHonouredFromAConfiguredProxy()
        {
            var resolved = await ResolvedClientIp(IPAddress.Parse("10.0.0.2"), SessionIp.ToString(),
                new ForwardedHeadersConfiguration { KnownNetworks = { "10.0.0.0/24" } });

            Assert.AreEqual(SessionIp, resolved);
        }
    }
}
