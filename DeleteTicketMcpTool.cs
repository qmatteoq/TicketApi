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
    public class DeleteTicketMcpTool
    {
        private readonly ILogger<DeleteTicketMcpTool> _logger;
        private readonly TableClient _tableClient;

        public DeleteTicketMcpTool(ILogger<DeleteTicketMcpTool> logger, IConfiguration configuration)
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

        [Function("DeleteTicketTool")]
        public async Task<string> Run(
            [McpToolTrigger("delete_ticket", "Delete a support ticket by ID")] ToolInvocationContext context,
            [McpToolProperty("id", "The ticket ID to delete", isRequired: true)] string id
        )
        {
            _logger.LogInformation("MCP Tool: Deleting ticket with ID: {TicketId}", id);

            if (string.IsNullOrEmpty(id))
            {
                var errorMessage = "Please provide a valid ticket ID.";
                _logger.LogWarning("MCP Tool: {ErrorMessage}", errorMessage);
                throw new ArgumentException(errorMessage);
            }

            try
            {
                // Retrieve the entity to confirm it exists
                var entity = await _tableClient.GetEntityAsync<TableEntity>("ticket", id);

                // Delete the entity
                await _tableClient.DeleteEntityAsync("ticket", id);

                var successMessage = $"Ticket with ID {id} deleted successfully.";
                _logger.LogInformation("MCP Tool: {SuccessMessage}", successMessage);
                return successMessage;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var notFoundMessage = $"Ticket with ID {id} not found.";
                _logger.LogWarning("MCP Tool: {NotFoundMessage}", notFoundMessage);
                throw new InvalidOperationException(notFoundMessage, ex);
            }
        }
    }
}
