using Azure;
using Azure.Data.Tables;
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

            var connectionString = configuration["AzureWebJobsStorage"];
            // Initialize the TableClient instance
            var serviceClient = new TableServiceClient(connectionString);
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
