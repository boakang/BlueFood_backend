namespace BlueFood.Api.Models;

public class AuditLogDto
{
    public long AuditId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public DateTime ActionAt { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string? PayloadText { get; set; }
}
