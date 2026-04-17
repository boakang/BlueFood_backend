namespace BlueFood.Api.Models;

public class PartnerDto
{
    public int PartnerId { get; set; }
    public byte PartnerType { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
