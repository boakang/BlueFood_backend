namespace BlueFood.Api.Models;

public class CertificateDto
{
    public long CertificateId { get; set; }
    public string CertificateCode { get; set; } = string.Empty;
    public string CertificateName { get; set; } = string.Empty;
    public string? IssuedBy { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiredDate { get; set; }
    public string? FileUrl { get; set; }
    public DateTime AttachedAt { get; set; }
    public string AttachedBy { get; set; } = string.Empty;
}
