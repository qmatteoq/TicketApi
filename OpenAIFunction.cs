using System.Net;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace TicketApi
{
    public class OpenAIFunction
    {
        [Function("GetOpenAIManifest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/ai-plugin.json")] HttpRequestData req)
        {
            var currentDomain = $"{req.Url.Scheme}://{req.Url.Host}:{req.Url.Port}";
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var result = File.ReadAllText(binDirectory + "/manifest/ai-plugin.json");
            var json = result.Replace("{url}", currentDomain);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(json);

            return response;
        }
    }
}
