using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using TicketApi.Services;

namespace TicketApi
{
    public class GetTicketsFunction
    {
        private readonly ILogger<GetTicketsFunction> _logger;
        private readonly TicketService _ticketService;

        public GetTicketsFunction(ILogger<GetTicketsFunction> logger, TicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
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

            var result = await _ticketService.GetTicketsAsync(search, assignedTo, status);

            return new OkObjectResult(result);
        }
    }
}
