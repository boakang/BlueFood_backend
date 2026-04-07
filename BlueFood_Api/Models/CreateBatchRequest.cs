namespace BlueFood.Api.Models;

public class CreateBatchRequest
{
    public string BatchCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int? FarmPartnerId { get; set; }
    public DateOnly? ProductionDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string TraceBaseUrl { get; set; } = "https://bluefood.local/trace/";
}
