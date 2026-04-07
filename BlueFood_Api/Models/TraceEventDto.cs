namespace BlueFood.Api.Models;

public class TraceEventDto
{
    public string BatchCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public string QRToken { get; set; } = string.Empty;
    public string TraceUrl { get; set; } = string.Empty;
    public int EventNo { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public string? FromPartnerName { get; set; }
    public string? ToPartnerName { get; set; }
    public string? LocationText { get; set; }
    public string? NoteText { get; set; }
}
