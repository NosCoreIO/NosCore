//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
//

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.GameObject.InterChannelCommunication.Hubs.AuthHub;
using NosCore.WebApi.Controllers;
using SpecLight;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NosCore.WebApi.Tests.ApiTests
{
    [TestClass]
    public class MallControllerTests
    {
        private const string Sid = "1";
        private const string Pid = "2";
        private const string UserId = "account";
        private const string CharacterName = "hero";
        private const string SessionIp = "10.0.0.5";

        private Mock<IAuthHub> AuthHub;
        private MallController Controller;
        private string Sas;
        private ActionResult? Result;
        private Exception? Error;

        [TestInitialize]
        public void Setup()
        {
            AuthHub = new Mock<IAuthHub>();
            AuthHub.Setup(s => s.GetSessionIpAsync(UserId)).ReturnsAsync(SessionIp);
            Controller = new MallController(AuthHub.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            Sas = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes($"{Sid}{Pid}{UserId}{CharacterName}shtmxpdlfeoqkr")));
        }

        [TestMethod]
        public async Task SignedRequestFromTheSessionAddressOpensTheMall()
        {
            await new Spec("Signed request from the session address opens the mall")
                .Given(RequestComesFrom_, SessionIp)
                .WhenAsync(OpeningTheMall)
                .Then(MallPageShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task OnlyTheFirstForwardedAddressIsCompared()
        {
            await new Spec("Only the first forwarded address is compared")
                .Given(RequestComesFrom_, $"{SessionIp}, 192.168.0.1")
                .WhenAsync(OpeningTheMall)
                .Then(MallPageShouldBeReturned)
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task WrongSignatureIsRefused()
        {
            await new Spec("Wrong signature is refused")
                .Given(RequestComesFrom_, SessionIp)
                .And(SignatureIsTampered)
                .WhenAsync(OpeningTheMall)
                .Then(RequestShouldBeRefusedFor_, "sas")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestFromAnotherAddressIsRefused()
        {
            await new Spec("Request from another address is refused")
                .Given(RequestComesFrom_, "10.0.0.6")
                .WhenAsync(OpeningTheMall)
                .Then(RequestShouldBeRefusedFor_, "X-Forwarded-For")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestWithoutForwardedAddressIsRefused()
        {
            await new Spec("Request without forwarded address is refused")
                .WhenAsync(OpeningTheMall)
                .Then(RequestShouldBeRefusedFor_, "X-Forwarded-For")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestWithUnreadableForwardedAddressIsRefused()
        {
            await new Spec("Request with unreadable forwarded address is refused")
                .Given(RequestComesFrom_, "not-an-address")
                .WhenAsync(OpeningTheMall)
                .Then(RequestShouldBeRefusedFor_, "X-Forwarded-For")
                .ExecuteAsync();
        }

        [TestMethod]
        public async Task RequestForAnAccountWithoutSessionIsRefused()
        {
            await new Spec("Request for an account without session is refused")
                .Given(RequestComesFrom_, SessionIp)
                .And(AccountHasNoSession)
                .WhenAsync(OpeningTheMall)
                .Then(RequestShouldBeRefusedFor_, "X-Forwarded-For")
                .ExecuteAsync();
        }

        private void RequestComesFrom_(string forwarded) =>
            Controller.HttpContext.Request.Headers["X-Forwarded-For"] = forwarded;

        private void SignatureIsTampered() => Sas = new string('0', Sas.Length);

        private void AccountHasNoSession() =>
            AuthHub.Setup(s => s.GetSessionIpAsync(UserId)).ReturnsAsync((string?)null);

        private async Task OpeningTheMall()
        {
            try
            {
                Result = await Controller.IndexAsync(Sid, Pid, UserId, CharacterName, Sas, "c", "shop", "en");
            }
            catch (ArgumentException ex)
            {
                Error = ex;
            }
        }

        private void MallPageShouldBeReturned()
        {
            Assert.IsNull(Error);
            var content = Result as ContentResult;
            Assert.IsNotNull(content);
            Assert.AreEqual("text/html; charset=UTF-8", content.ContentType);
            StringAssert.Contains(content.Content, CharacterName);
            StringAssert.Contains(content.Content, Sas);
        }

        private void RequestShouldBeRefusedFor_(string parameter)
        {
            Assert.IsNull(Result);
            Assert.AreEqual(parameter, (Error as ArgumentException)?.ParamName);
        }
    }
}
