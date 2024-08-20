using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace TicketApi
{
    public class GetTicketsFunction
    {
        private readonly ILogger<GetTicketsFunction> _logger;

        public GetTicketsFunction(ILogger<GetTicketsFunction> logger)
        {
            _logger = logger;
        }

        [Function("GetTickets")]
        [OpenApiOperation(operationId: "GetTickets", Description = "Get the tickets with a given keyword in the title")]
        [OpenApiParameter(name: "search", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "The search keyword")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<Ticket>), Description = "OK")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tickets")] HttpRequestData req,
            [TableInput("TicketTable", "ticket")] List<MyTicketTable> tickets, [FromQuery] string search)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            List<Ticket> result = new List<Ticket>();

            if (string.IsNullOrEmpty(search))
            {
                result = tickets.Select(x => new Ticket
                {
                    Id = x.RowKey,
                    Title = x.Title,
                    Description = x.Description,
                    AssignedTo = x.AssignedTo,
                    Severity = x.Severity
                }).ToList();
            }
            else
            {
                result = tickets.Where(t => t.Title.ToLowerInvariant().Contains(search.ToLowerInvariant())).Select(x => new Ticket
                {
                    Id = x.RowKey,
                    Title = x.Title,
                    Description = x.Description,
                    AssignedTo = x.AssignedTo,
                    Severity = x.Severity
                }).ToList();
            }

            return new OkObjectResult(result);
        }
    }
}
