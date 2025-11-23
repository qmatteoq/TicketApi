using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using TicketApi.Services;

namespace TicketApi
{
    public class GetTicketsMcpTool
    {
        private readonly ILogger<GetTicketsMcpTool> _logger;
        private readonly TicketService _ticketService;

        public GetTicketsMcpTool(ILogger<GetTicketsMcpTool> logger, TicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
        }

        [Function("GetTicketsTool")]
        public async Task<List<Ticket>> Run(
            [McpToolTrigger("get_tickets", "Get tickets with optional filtering by search keyword, assignee, or status")] ToolInvocationContext context,
            [McpToolProperty("search", "Search keyword to find in title or assignedTo field", isRequired: false)] string? search = null,
            [McpToolProperty("assignedTo", "Filter by person assigned to the ticket", isRequired: false)] string? assignedTo = null,
            [McpToolProperty("status", "Filter by ticket status", isRequired: false)] string? status = null
        )
        {
            _logger.LogInformation("MCP Tool: Getting tickets with search: {Search}, assignedTo: {AssignedTo}, status: {Status}", 
                search ?? "none", assignedTo ?? "none", status ?? "none");

            return await _ticketService.GetTicketsAsync(search, assignedTo, status);
        }
    }
}
