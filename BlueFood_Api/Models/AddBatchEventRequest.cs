namespace BlueFood.Api.Models;

public class AddBatchEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public int? FromPartnerId { get; set; }
    public int? ToPartnerId { get; set; }
    public string? LocationText { get; set; }
    public string? NoteText { get; set; }
    public string Actor { get; set; } = string.Empty;
}
