using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TicketApi.Services
{
    public class TicketService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<TicketService> _logger;

        public TicketService(ILogger<TicketService> logger, IConfiguration configuration)
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

        public async Task<MyTicketTable> CreateTicketAsync(string title, string description, string assignedTo, string severity, string status, string? id = null)
        {
            _logger.LogInformation("Creating ticket with title: {Title}", title);
            
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
                _logger.LogInformation("Successfully created ticket with ID: {TicketId}", ticketId);
                return ticketTable;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to create ticket");
                throw;
            }
        }

        public async Task<string> DeleteTicketAsync(string id)
        {
            _logger.LogInformation("Deleting ticket with ID: {TicketId}", id);

            if (string.IsNullOrEmpty(id))
            {
                var errorMessage = "Please provide a valid ticket ID.";
                _logger.LogWarning("{ErrorMessage}", errorMessage);
                throw new ArgumentException(errorMessage);
            }

            try
            {
                // Retrieve the entity to confirm it exists
                var entity = await _tableClient.GetEntityAsync<TableEntity>("ticket", id);

                // Delete the entity
                await _tableClient.DeleteEntityAsync("ticket", id);

                var successMessage = $"Ticket with ID {id} deleted successfully.";
                _logger.LogInformation("{SuccessMessage}", successMessage);
                return successMessage;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var notFoundMessage = $"Ticket with ID {id} not found.";
                _logger.LogWarning("{NotFoundMessage}", notFoundMessage);
                throw new InvalidOperationException(notFoundMessage, ex);
            }
        }

        public async Task<List<Ticket>> GetTicketsAsync(string? search = null, string? assignedTo = null, string? status = null)
        {
            _logger.LogInformation("Getting tickets with search: {Search}, assignedTo: {AssignedTo}, status: {Status}", 
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

            _logger.LogInformation("Returning {Count} tickets", result.Count);
            return result;
        }
    }
}
