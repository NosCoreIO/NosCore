using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NosCore.Core.Controllers
{
    public class GraphQlParameter
    {
        public string Query { get; set; }
    }

    [AllowAnonymous]
    [Route("api/graphql")]
    public class GraphQlController : Controller
    {
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ISchema _schema;

        public GraphQlController(IDocumentExecuter documentExecuter, ISchema schema)
        {
            _documentExecuter = documentExecuter;
            _schema = schema;
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] GraphQlParameter query)
        {
            var executionOptions = new ExecutionOptions { Schema = _schema, Query = query.Query, UserContext = User };
            var result = await _documentExecuter.ExecuteAsync(executionOptions).ConfigureAwait(false);

            if (result.Errors?.Count > 0)
            {
                return BadRequest(result.Errors);
            }

            return Ok(result);
        }
    }
}
