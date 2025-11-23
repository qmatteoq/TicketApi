using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using TicketApi.Services;

namespace TicketApi
{
    public class DeleteTicketMcpTool
    {
        private readonly ILogger<DeleteTicketMcpTool> _logger;
        private readonly TicketService _ticketService;

        public DeleteTicketMcpTool(ILogger<DeleteTicketMcpTool> logger, TicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
        }

        [Function("DeleteTicketTool")]
        public async Task<string> Run(
            [McpToolTrigger("delete_ticket", "Delete a support ticket by ID")] ToolInvocationContext context,
            [McpToolProperty("id", "The ticket ID to delete", isRequired: true)] string id
        )
        {
            _logger.LogInformation("MCP Tool: Deleting ticket with ID: {TicketId}", id);

            return await _ticketService.DeleteTicketAsync(id);
        }
    }
}
