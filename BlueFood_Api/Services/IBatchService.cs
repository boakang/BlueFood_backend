using BlueFood.Api.Models;

namespace BlueFood.Api.Services;

public interface IBatchService
{
    Task<CreateBatchResult> CreateBatchAsync(CreateBatchRequest request, CancellationToken cancellationToken);
    Task AddBatchEventAsync(string batchCode, AddBatchEventRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TraceEventDto>> GetTraceByBatchCodeAsync(string batchCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<TraceEventDto>> GetTraceByQrTokenAsync(string qrToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditLogDto>> GetAuditByBatchCodeAsync(string batchCode, CancellationToken cancellationToken);
    Task<long> CreateCertificateAsync(CreateCertificateRequest request, CancellationToken cancellationToken);
    Task AttachCertificateAsync(string batchCode, AttachCertificateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CertificateDto>> GetCertificatesByBatchCodeAsync(string batchCode, CancellationToken cancellationToken);
}
