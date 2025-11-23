using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using TicketApi.Services;

namespace TicketApi
{
    public class CreateTicketFunction
    {
        private readonly ILogger<CreateTicketFunction> _logger;
        private readonly TicketService _ticketService;

        public CreateTicketFunction(ILogger<CreateTicketFunction> logger, TicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
        }

        [Function("CreateTicket")]
        [OpenApiOperation(operationId: "CreateTicket", Description = "Create a new ticket")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(Ticket), Required = true, Description = "The ticket to create")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(MyTicketTable), Description = "OK")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "tickets")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            var ticket = await req.ReadFromJsonAsync<Ticket>();

            if (ticket == null)
            {
                return new BadRequestObjectResult("Invalid ticket data.");
            }

            try
            {
                var result = await _ticketService.CreateTicketAsync(
                    ticket.Title,
                    ticket.Description,
                    ticket.AssignedTo,
                    ticket.Severity,
                    ticket.Status,
                    ticket.Id
                );
                
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
    }
}
