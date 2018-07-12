using System.Net.Http.Formatting;
using System.Reflection;
using System.Web.Http;
using System.Web.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace TestCurrentContext
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            GlobalConfiguration.Configuration.Formatters.Clear();
            var jsonFormatter = new JsonMediaTypeFormatter
            {
                SerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    Formatting = Formatting.Indented
                }
            };
            GlobalConfiguration.Configuration.Formatters.Add(jsonFormatter);


            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithHttpRequestId()
                .Enrich.WithUserName()
                .Enrich.WithProperty("WebApiAssembly", Assembly.GetAssembly(typeof(System.Web.Http.ApiController)).FullName)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();
        }
    }
}
