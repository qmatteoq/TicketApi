using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using System.Net;
using TicketApi.Services;

namespace TicketApi
{
    public class DeleteTicketFunction
    {
        private readonly ILogger<DeleteTicketFunction> _logger;
        private readonly TicketService _ticketService;

        public DeleteTicketFunction(ILogger<DeleteTicketFunction> logger, TicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
        }

        [Function("DeleteTicketFunction")]
        [OpenApiOperation(operationId: "DeleteTicket", Description = "Delete the ticket given an id")]
        [OpenApiParameter(name: "id", In = ParameterLocation.Path, Required = true, Type = typeof(string), Description = "The ticket id")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.OK, Description = "Ticket deleted")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "tickets/{id}")] HttpRequest req, string id)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to delete a ticket.");

            try
            {
                var result = await _ticketService.DeleteTicketAsync(id);
                return new OkObjectResult(result);
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return new NotFoundObjectResult(ex.Message);
            }
        }
    }
}