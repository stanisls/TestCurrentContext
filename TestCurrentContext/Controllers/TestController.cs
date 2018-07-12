using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using Serilog;
using TestCurrentContext.Filters;

namespace TestCurrentContext.Controllers
{
    [ApiBasicAuthentication]
    [RoutePrefix("test")]
    public class TestController : ApiController
    {
        private readonly ILogger _logger;
        public TestController()
        {
            _logger = Log.Logger.ForContext<TestController>();
        }

        [Route("")]
        [HttpGet]
        [ResponseType(typeof(string))]
        [Authorize(Roles = "Tester")]
        public IHttpActionResult Test()
        {
            _logger.Information("Test action called");
            return Ok("Success");
        }
    }
}
