using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace TicketApi
{
    public class CreateTicketFunction
    {
        private readonly ILogger<CreateTicketFunction> _logger;
        private readonly TableClient _tableClient;

        public CreateTicketFunction(ILogger<CreateTicketFunction> logger, IConfiguration configuration)
        {
            _logger = logger;

            // Initialize the TableClient instance
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

        [Function("CreateTicket")]
        [OpenApiOperation(operationId: "CreateTicket", Description = "Create a new ticket")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Ticket), Required = true, Description = "The ticket to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MyTicketTable), Description = "OK")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tickets")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var ticket = await req.ReadFromJsonAsync<Ticket>();

            var id = string.IsNullOrEmpty(ticket.Id) ? Guid.NewGuid().ToString() : ticket.Id;
            
            MyTicketTable ticketTable = new MyTicketTable
            {
                PartitionKey = "ticket",
                RowKey = id,
                Title = ticket.Title,
                Description = ticket.Description,
                AssignedTo = ticket.AssignedTo,
                Severity = ticket.Severity,
                Status = ticket.Status,
                Timestamp = DateTimeOffset.UtcNow
            };

            try
            {
                await _tableClient.UpsertEntityAsync(ticketTable);
                return new OkObjectResult(ticketTable);
            }
            catch (RequestFailedException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
