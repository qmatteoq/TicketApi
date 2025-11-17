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
    public class GetTicketsMcpTool
    {
        private readonly ILogger<GetTicketsMcpTool> _logger;
        private readonly TableClient _tableClient;

        public GetTicketsMcpTool(ILogger<GetTicketsMcpTool> logger, IConfiguration configuration)
        {
            _logger = logger;

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

            string partitionKey = "ticket";

            // Query the table with the specified partition key
            var queryResults = _tableClient.QueryAsync<MyTicketTable>(filter: $"PartitionKey eq '{partitionKey}'");

            List<MyTicketTable> tickets = new List<MyTicketTable>();
            await foreach (var ticket in queryResults)
            {
                tickets.Add(ticket);
            }

            List<Ticket> result = new List<Ticket>();

            if (string.IsNullOrEmpty(search))
            {
                result = tickets.Select(x => new Ticket
                {
                    Id = x.RowKey,
                    Title = x.Title,
                    Description = x.Description,
                    AssignedTo = x.AssignedTo,
                    Severity = x.Severity,
                    CreatedAt = x.Timestamp,
                    Status = x.Status
                }).ToList();
            }
            else
            {
                result = tickets.Where(t => t.Title.ToLowerInvariant().Contains(search.ToLowerInvariant()) || 
                    (!string.IsNullOrEmpty(t.AssignedTo) && t.AssignedTo.ToLowerInvariant().Contains(search.ToLowerInvariant())))
                    .Select(x => new Ticket
                    {
                        Id = x.RowKey,
                        Title = x.Title,
                        Description = x.Description,
                        AssignedTo = x.AssignedTo,
                        Severity = x.Severity,
                        CreatedAt = x.Timestamp,
                        Status = x.Status
                    }).ToList();
            }

            if (!string.IsNullOrEmpty(assignedTo))
            {
                result = result.Where(t => !string.IsNullOrEmpty(t.AssignedTo) && 
                    t.AssignedTo.ToLowerInvariant().Contains(assignedTo.ToLowerInvariant())).ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                result = result.Where(t => !string.IsNullOrEmpty(t.Status) && 
                    t.Status.ToLowerInvariant().Contains(status.ToLowerInvariant())).ToList();
            }

            _logger.LogInformation("MCP Tool: Returning {Count} tickets", result.Count);
            return result;
        }
    }
}
