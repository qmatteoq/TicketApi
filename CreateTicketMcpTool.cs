using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TicketApi
{
    public class CreateTicketMcpTool
    {
        private readonly ILogger<CreateTicketMcpTool> _logger;
        private readonly TableClient _tableClient;

        public CreateTicketMcpTool(ILogger<CreateTicketMcpTool> logger, IConfiguration configuration)
        {
            _logger = logger;

            // Initialize the TableClient instance
            var connectionString = configuration["StorageConnectionString"];
            TableServiceClient serviceClient;
            
            // Use connection string for local development, DefaultAzureCredential for cloud
            if (!string.IsNullOrEmpty(connectionString))
            {
                serviceClient = new TableServiceClient(connectionString,
                    new TableClientOptions
                    {
                        Retry = { Mode = RetryMode.Exponential, MaxRetries = 10, Delay = TimeSpan.FromSeconds(3) },
                        Transport = new HttpClientTransport(new HttpClient
                        {
                            Timeout = TimeSpan.FromSeconds(60)
                        })
                    });
            }
            else
            {
                var tableEndpoint = configuration["TableEndpoint"];
                var credential = new DefaultAzureCredential();
                serviceClient = new TableServiceClient(new Uri(tableEndpoint ?? throw new InvalidOperationException("TableEndpoint configuration is missing")), credential,
                    new TableClientOptions
                    {
                        Retry = { Mode = RetryMode.Exponential, MaxRetries = 10, Delay = TimeSpan.FromSeconds(3) },
                        Transport = new HttpClientTransport(new HttpClient
                        {
                            Timeout = TimeSpan.FromSeconds(60)
                        })
                    });
            }
                
            _tableClient = serviceClient.GetTableClient("TicketTable");
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
            
            var ticketId = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString() : id;
            
            MyTicketTable ticketTable = new MyTicketTable
            {
                PartitionKey = "ticket",
                RowKey = ticketId,
                Title = title,
                Description = description,
                AssignedTo = assignedTo,
                Severity = severity,
                Status = status,
                Timestamp = DateTimeOffset.UtcNow
            };

            try
            {
                // Ensure the table exists
                await _tableClient.CreateIfNotExistsAsync();

                await _tableClient.UpsertEntityAsync(ticketTable);
                _logger.LogInformation("MCP Tool: Successfully created ticket with ID: {TicketId}", ticketId);
                return ticketTable;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "MCP Tool: Failed to create ticket");
                throw;
            }
        }
    }
}
