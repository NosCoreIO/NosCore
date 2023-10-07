using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NosCore.MasterServer.Controllers
{
    [Route("[controller]")]
    [AllowAnonymous]
    // this file is an entrypoint for Mall creation. it's not recommended to add code here.
    public class MallController : Controller
    {
        [HttpGet]
        [Route("")]
        public ActionResult Index(string sid, string pid, string user_id, string m_szName, string sas, string c, string shopType,
            string display_language)
        {
            using var md5 = MD5.Create();
            var md5bytes = md5.ComputeHash(Encoding.Default.GetBytes($"{sid}{pid}{user_id}{m_szName}shtmxpdlfeoqkr"));

            if (sas != Convert.ToHexString(md5bytes))
            {
                throw new ArgumentException(null, nameof(sas));
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
