using System.Text.Json.Serialization;

public class PostTicket 
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("assignedTo")]
    public required string AssignedTo { get; set; }

    [JsonPropertyName("severity")]
    public required string Severity { get; set; }
}