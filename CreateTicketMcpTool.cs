using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using TicketApi.Services;

namespace TicketApi
{
    public class CreateTicketMcpTool
    {
        private readonly ILogger<CreateTicketMcpTool> _logger;
        private readonly TicketService _ticketService;

        public CreateTicketMcpTool(ILogger<CreateTicketMcpTool> logger, TicketService ticketService)
        {
            _logger = logger;
            _ticketService = ticketService;
        }

        [Function("CreateTicketTool")]
        public async Task<MyTicketTable> Run(
            [McpToolTrigger("create_ticket", "Create a new support ticket")] ToolInvocationContext context,
            [McpToolProperty("title", "The ticket title", isRequired: true)] string title,
            [McpToolProperty("description", "The ticket description", isRequired: true)] string description,
            [McpToolProperty("assignedTo", "Person assigned to the ticket", isRequired: true)] string assignedTo,
            [McpToolProperty("severity", "Severity level (e.g., Low, Medium, High, Critical)", isRequired: true)] string severity,
            [McpToolProperty("status", "Current status (e.g., Open, In Progress, Closed)", isRequired: true)] string status,
            [McpToolProperty("id", "Optional ticket ID (auto-generated if not provided)", isRequired: false)] string? id = null
        )
        {
            _logger.LogInformation("MCP Tool: Creating ticket with title: {Title}", title);
            
            return await _ticketService.CreateTicketAsync(title, description, assignedTo, severity, status, id);
        }
    }
}
