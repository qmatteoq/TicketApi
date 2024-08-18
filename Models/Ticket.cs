using System.Text.Json.Serialization;

public class Ticket 
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("assignedTo")]
    public required string AssignedTo { get; set; }

    [JsonPropertyName("severity")]
    public required string Severity { get; set; }
}