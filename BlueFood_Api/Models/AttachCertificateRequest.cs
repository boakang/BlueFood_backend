namespace BlueFood.Api.Models;

public class AttachCertificateRequest
{
    public long CertificateId { get; set; }
    public string Actor { get; set; } = string.Empty;
}
