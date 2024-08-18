using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;

namespace TicketApi
{
    public class CreateTicketFunction
    {
        private readonly ILogger<CreateTicketFunction> _logger;

        public CreateTicketFunction(ILogger<CreateTicketFunction> logger)
        {
            _logger = logger;
        }

        [Function("CreateTicket")]
        [TableOutput("TicketTable", "ticket", Connection = "AzureWebJobsStorage")]
        [OpenApiOperation(operationId: "CreateTicket", Description = "Create a new ticket")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(PostTicket), Required = true, Description = "The ticket to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MyTicketTable), Description = "OK")]
        public async Task<MyTicketTable> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tickets")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var ticket = await req.ReadFromJsonAsync<PostTicket>();

            MyTicketTable ticketTable = new MyTicketTable
            {
                PartitionKey = "ticket",
                RowKey = Guid.NewGuid().ToString(),
                Title = ticket.Title,
                Description = ticket.Description,
                AssignedTo = ticket.AssignedTo,
                Severity = ticket.Severity
            };

            return ticketTable;
        }
    }
}
