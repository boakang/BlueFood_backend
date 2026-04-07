namespace BlueFood.Api.Models;

public class CreateBatchResult
{
    public Guid BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string QRToken { get; set; } = string.Empty;
    public string TraceUrl { get; set; } = string.Empty;
}
