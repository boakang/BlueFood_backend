namespace BlueFood.Api.Models;

public class CreateCertificateRequest
{
    public string CertificateCode { get; set; } = string.Empty;
    public string CertificateName { get; set; } = string.Empty;
    public string? IssuedBy { get; set; }
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiredDate { get; set; }
    public string? FileUrl { get; set; }
    public string Actor { get; set; } = string.Empty;
}
