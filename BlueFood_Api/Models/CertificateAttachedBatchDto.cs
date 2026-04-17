namespace BlueFood.Api.Models;

public class CertificateAttachedBatchDto
{
    public Guid BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime AttachedAt { get; set; }
    public string AttachedBy { get; set; } = string.Empty;
}
