namespace BlueFood.Api.Models;

public class CertificateManagementRowDto
{
    public long CertificateId { get; set; }
    public string CertificateCode { get; set; } = string.Empty;
    public string CertificateName { get; set; } = string.Empty;
    public string? IssuedBy { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiredDate { get; set; }
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AttachedBatchCount { get; set; }
    public DateTime? LastAttachedAt { get; set; }
}