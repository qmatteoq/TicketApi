using System.Net;
using Azure.Core.Pipeline;
using Azure.Core;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Configuration;
using Azure.Identity;

namespace TicketApi
{
    public class GetTicketsFunction
    {
        private readonly ILogger<GetTicketsFunction> _logger;
        private readonly TableClient _tableClient;

        public GetTicketsFunction(ILogger<GetTicketsFunction> logger, IConfiguration configuration)
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

        [Function("GetTickets")]
        [OpenApiOperation(operationId: "GetTickets", Description = "Get the tickets with a given keyword in the title or assigned to a specific person")]
        [OpenApiParameter(name: "search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The search keyword")]
        [OpenApiParameter(name: "assignedTo", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The person assigned to the ticket")]
        [OpenApiParameter(name: "status", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The status of the ticket")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<Ticket>), Description = "OK")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tickets")] HttpRequestData req,
            [FromQuery] string search, [FromQuery] string assignedTo, [FromQuery] string status)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string partitionKey = "ticket";

            // Query the table with the specified partition key
            var queryResults = _tableClient.QueryAsync<MyTicketTable>(filter: $"PartitionKey eq '{partitionKey}'");

            List<MyTicketTable> tickets = new List<MyTicketTable>();
            await foreach (var ticket in queryResults)
            {
                tickets.Add(ticket);
            }

            List<Ticket> result = new List<Ticket>();

            if (string.IsNullOrEmpty(search))
            {
                result = tickets.Select(x => new Ticket
                {
                    Id = x.RowKey,
                    Title = x.Title,
                    Description = x.Description,
                    AssignedTo = x.AssignedTo,
                    Severity = x.Severity,
                    CreatedAt = x.Timestamp,
                    Status = x.Status
                }).ToList();
            }
            else
            {
                result = tickets.Where(t => t.Title.ToLowerInvariant().Contains(search.ToLowerInvariant()) || (!string.IsNullOrEmpty(t.AssignedTo) && t.AssignedTo.ToLowerInvariant().Contains(search.ToLowerInvariant()))).Select(x => new Ticket
                {
                    Id = x.RowKey,
                    Title = x.Title,
                    Description = x.Description,
                    AssignedTo = x.AssignedTo,
                    Severity = x.Severity,
                    CreatedAt = x.Timestamp,
                    Status = x.Status
                }).ToList();
            }

            if (!string.IsNullOrEmpty(assignedTo))
            {
                result = result.Where(t => !string.IsNullOrEmpty(t.AssignedTo) && t.AssignedTo.ToLowerInvariant().Contains(assignedTo.ToLowerInvariant())).ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                result = result.Where(t => !string.IsNullOrEmpty(t.Status) && t.Status.ToLowerInvariant().Contains(status.ToLowerInvariant())).ToList();
            }

            return new OkObjectResult(result);
        }
    }
}
