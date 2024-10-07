using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Data.Tables;
using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using Azure.Core.Pipeline;
using Azure.Core;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Azure.Identity;

namespace TicketApi
{
    public class DeleteTicketFunction
    {
        private readonly ILogger<DeleteTicketFunction> _logger;
        private readonly TableClient _tableClient;

        public DeleteTicketFunction(ILogger<DeleteTicketFunction> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            var tableEndpoint = configuration["TableEndpoint"];
            var credential = new DefaultAzureCredential();
            var serviceClient = new TableServiceClient(new Uri(tableEndpoint), credential,
                 new TableClientOptions
                 {
                     Retry = { Mode = RetryMode.Exponential, MaxRetries = 10, Delay = TimeSpan.FromSeconds(3) },
                     Transport = new HttpClientTransport(new HttpClient
                     {
                         Timeout = TimeSpan.FromSeconds(60)
                     })
                 });

            _tableClient = serviceClient.GetTableClient("TicketTable");
        }

        [Function("DeleteTicketFunction")]
        [OpenApiOperation(operationId: "DeleteTicket", Description = "Delete the ticket given an id")]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The ticket id")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Ticket deleted")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tickets/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to delete a ticket.");

            if (string.IsNullOrEmpty(id))
            {
                return new BadRequestObjectResult("Please provide a valid ID.");
            }

            try
            {
                // Retrieve the entity
                var entity = await _tableClient.GetEntityAsync<TableEntity>("ticket", id);

                // Delete the entity
                await _tableClient.DeleteEntityAsync("ticket", id);

                return new OkObjectResult($"Ticket with ID {id} deleted successfully.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return new NotFoundObjectResult($"Ticket with ID {id} not found.");
            }
        }
    }
}