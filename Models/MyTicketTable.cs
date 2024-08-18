using Azure;
using Azure.Data.Tables;

public class MyTicketTable : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string AssignedTo { get; set; }

    public string Severity { get; set; }
}