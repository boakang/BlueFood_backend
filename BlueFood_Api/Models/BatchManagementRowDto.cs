namespace BlueFood.Api.Models;

public class BatchManagementRowDto
{
    public Guid BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? FarmPartnerName { get; set; }
    public int EventCount { get; set; }
    public DateTime? LastEventTime { get; set; }
    public int CertificateCount { get; set; }
    public string? CertificateName { get; set; }
}