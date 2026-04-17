using System.Data;
using BlueFood.Api.Infrastructure;
using BlueFood.Api.Models;
using Microsoft.Data.SqlClient;

namespace BlueFood.Api.Services;

public class BatchService : IBatchService
{
    private readonly string _connectionString;

    public BatchService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("BlueFoodDb")
            ?? throw new InvalidOperationException("Missing connection string: BlueFoodDb");
    }

    public async Task<CreateBatchResult> CreateBatchAsync(CreateBatchRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_CreateBatch", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@BatchCode", request.BatchCode);
        command.Parameters.AddWithValue("@ProductName", request.ProductName);
        command.Parameters.AddWithValue("@FarmPartnerId", (object?)request.FarmPartnerId ?? DBNull.Value);
        command.Parameters.AddWithValue("@ProductionDate", (object?)request.ProductionDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExpiryDate", (object?)request.ExpiryDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@Actor", request.Actor);
        command.Parameters.AddWithValue("@TraceBaseUrl", request.TraceBaseUrl);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Create batch did not return any result.");
        }

        return new CreateBatchResult
        {
            BatchId = reader.GetGuid(reader.GetOrdinal("BatchId")),
            BatchCode = reader.GetString(reader.GetOrdinal("BatchCode")),
            QRToken = reader.GetString(reader.GetOrdinal("QRToken")),
            TraceUrl = PublicTraceUrlBuilder.Build(reader.GetString(reader.GetOrdinal("QRToken")))
        };
    }

    public async Task AddBatchEventAsync(string batchCode, AddBatchEventRequest request, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_AddBatchEvent", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@BatchCode", batchCode);
        command.Parameters.AddWithValue("@EventType", request.EventType);
        command.Parameters.AddWithValue("@FromPartnerId", (object?)request.FromPartnerId ?? DBNull.Value);
        command.Parameters.AddWithValue("@ToPartnerId", (object?)request.ToPartnerId ?? DBNull.Value);
        command.Parameters.AddWithValue("@LocationText", (object?)request.LocationText ?? DBNull.Value);
        command.Parameters.AddWithValue("@NoteText", (object?)request.NoteText ?? DBNull.Value);
        command.Parameters.AddWithValue("@Actor", request.Actor);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(CancellationToken cancellationToken)
    {
        var result = new DashboardOverviewDto();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_GetDashboardOverview", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            result.TotalBatches = reader.GetInt32(reader.GetOrdinal("TotalBatches"));
            result.TotalTraceEvents = reader.GetInt32(reader.GetOrdinal("TotalTraceEvents"));
            result.TotalCertificatesAttached = reader.GetInt32(reader.GetOrdinal("TotalCertificatesAttached"));
        }

        var eventTypeDistribution = new List<DashboardChartItemDto>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                eventTypeDistribution.Add(new DashboardChartItemDto
                {
                    Label = reader.GetString(reader.GetOrdinal("Label")),
                    Value = reader.GetInt32(reader.GetOrdinal("Value"))
                });
            }
        }

        var timelineSeries = new List<DashboardChartItemDto>();
        if (await reader.NextResultAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                timelineSeries.Add(new DashboardChartItemDto
                {
                    Label = reader.GetString(reader.GetOrdinal("Label")),
                    Value = reader.GetInt32(reader.GetOrdinal("Value"))
                });
            }
        }

        result.EventTypeDistribution = eventTypeDistribution;
        result.TimelineSeries = timelineSeries;
        return result;
    }

    public async Task<IReadOnlyList<PartnerDto>> GetPartnersAsync(int? partnerType, bool onlyActive, CancellationToken cancellationToken)
    {
        var list = new List<PartnerDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_GetPartners", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@PartnerType", (object?)partnerType ?? DBNull.Value);
        command.Parameters.AddWithValue("@OnlyActive", onlyActive);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PartnerDto
            {
                PartnerId = reader.GetInt32(reader.GetOrdinal("PartnerId")),
                PartnerType = reader.GetByte(reader.GetOrdinal("PartnerType")),
                PartnerCode = reader.GetString(reader.GetOrdinal("PartnerCode")),
                PartnerName = reader.GetString(reader.GetOrdinal("PartnerName")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<BatchManagementRowDto>> GetBatchManagementAsync(string? keyword, int take, CancellationToken cancellationToken)
    {
        var list = new List<BatchManagementRowDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_GetBatchManagement", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Keyword", (object?)keyword ?? DBNull.Value);
        command.Parameters.AddWithValue("@Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new BatchManagementRowDto
            {
                BatchId = reader.GetGuid(reader.GetOrdinal("BatchId")),
                BatchCode = reader.GetString(reader.GetOrdinal("BatchCode")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                CurrentStatus = reader.GetString(reader.GetOrdinal("CurrentStatus")),
                CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                FarmPartnerName = reader.IsDBNull(reader.GetOrdinal("FarmPartnerName")) ? null : reader.GetString(reader.GetOrdinal("FarmPartnerName")),
                EventCount = reader.GetInt32(reader.GetOrdinal("EventCount")),
                LastEventTime = reader.IsDBNull(reader.GetOrdinal("LastEventTime")) ? null : reader.GetDateTime(reader.GetOrdinal("LastEventTime")),
                CertificateCount = reader.GetInt32(reader.GetOrdinal("CertificateCount")),
                CertificateName = reader.IsDBNull(reader.GetOrdinal("CertificateName")) ? null : reader.GetString(reader.GetOrdinal("CertificateName"))
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<CertificateManagementRowDto>> GetCertificateManagementAsync(string? keyword, int take, CancellationToken cancellationToken)
    {
        var list = new List<CertificateManagementRowDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_GetCertificateManagement", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@Keyword", (object?)keyword ?? DBNull.Value);
        command.Parameters.AddWithValue("@Take", take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new CertificateManagementRowDto
            {
                CertificateId = reader.GetInt64(reader.GetOrdinal("CertificateId")),
                CertificateCode = reader.GetString(reader.GetOrdinal("CertificateCode")),
                CertificateName = reader.GetString(reader.GetOrdinal("CertificateName")),
                IssuedBy = reader.IsDBNull(reader.GetOrdinal("IssuedBy")) ? null : reader.GetString(reader.GetOrdinal("IssuedBy")),
                IssuedDate = reader.IsDBNull(reader.GetOrdinal("IssuedDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("IssuedDate"))),
                ExpiredDate = reader.IsDBNull(reader.GetOrdinal("ExpiredDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("ExpiredDate"))),
                FileUrl = reader.IsDBNull(reader.GetOrdinal("FileUrl")) ? null : reader.GetString(reader.GetOrdinal("FileUrl")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                AttachedBatchCount = reader.GetInt32(reader.GetOrdinal("AttachedBatchCount")),
                LastAttachedAt = reader.IsDBNull(reader.GetOrdinal("LastAttachedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("LastAttachedAt"))
            });
        }

        return list;
    }

    public async Task<IReadOnlyList<CertificateAttachedBatchDto>> GetBatchesByCertificateIdAsync(long certificateId, CancellationToken cancellationToken)
    {
        var list = new List<CertificateAttachedBatchDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("scm.usp_GetBatchesByCertificateId", connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.AddWithValue("@CertificateId", certificateId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new CertificateAttachedBatchDto
            {
                BatchId = reader.GetGuid(reader.GetOrdinal("BatchId")),
                BatchCode = reader.GetString(reader.GetOrdinal("BatchCode")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                CurrentStatus = reader.GetString(reader.GetOrdinal("CurrentStatus")),
                AttachedAt = reader.GetDateTime(reader.GetOrdinal("AttachedAt")),
                AttachedBy = reader.GetString(reader.GetOrdinal("AttachedBy"))
            });
        }

        return list;
    }

    public Task<IReadOnlyList<TraceEventDto>> GetTraceByBatchCodeAsync(string batchCode, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT BatchCode, ProductName, CurrentStatus, ISNULL(QRToken, '') AS QRToken, ISNULL(TraceUrl, '') AS TraceUrl,
       EventNo, EventType, EventTime, FromPartnerName, ToPartnerName, LocationText, NoteText
FROM scm.vw_BatchTrace
WHERE BatchCode = @BatchCode
ORDER BY EventNo;";

        return QueryTraceAsync(sql, new SqlParameter("@BatchCode", batchCode), cancellationToken);
    }

    public Task<IReadOnlyList<TraceEventDto>> GetTraceByQrTokenAsync(string qrToken, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT t.BatchCode, t.ProductName, t.CurrentStatus, ISNULL(t.QRToken, '') AS QRToken, ISNULL(t.TraceUrl, '') AS TraceUrl,
       t.EventNo, t.EventType, t.EventTime, t.FromPartnerName, t.ToPartnerName, t.LocationText, t.NoteText
FROM scm.vw_BatchTrace t
WHERE t.QRToken = @QrToken
ORDER BY t.EventNo;";

        return QueryTraceAsync(sql, new SqlParameter("@QrToken", qrToken), cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAuditByBatchCodeAsync(string batchCode, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TOP(200) a.AuditId, a.EntityName, a.EntityId, a.ActionType, a.ActionAt, a.Actor, a.PayloadText
FROM audit.AuditLogs a
WHERE a.PayloadText LIKE @BatchCodeFilter
ORDER BY a.AuditId DESC;";

        var list = new List<AuditLogDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchCodeFilter", $"%{batchCode}%");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AuditLogDto
            {
                AuditId = reader.GetInt64(reader.GetOrdinal("AuditId")),
                EntityName = reader.GetString(reader.GetOrdinal("EntityName")),
                EntityId = reader.GetString(reader.GetOrdinal("EntityId")),
                ActionType = reader.GetString(reader.GetOrdinal("ActionType")),
                ActionAt = reader.GetDateTime(reader.GetOrdinal("ActionAt")),
                Actor = reader.GetString(reader.GetOrdinal("Actor")),
                PayloadText = reader.IsDBNull(reader.GetOrdinal("PayloadText")) ? null : reader.GetString(reader.GetOrdinal("PayloadText"))
            });
        }

        return list;
    }

    public async Task<long> CreateCertificateAsync(CreateCertificateRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
BEGIN TRAN;

DECLARE @Now DATETIME2(3) = CONVERT(datetime2(3), SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
DECLARE @NewCertificateId TABLE (CertificateId BIGINT);

INSERT INTO scm.Certificates(CertificateCode, CertificateName, IssuedBy, IssuedDate, ExpiredDate, FileUrl, CreatedAt)
OUTPUT inserted.CertificateId INTO @NewCertificateId(CertificateId)
VALUES(@CertificateCode, @CertificateName, @IssuedBy, @IssuedDate, @ExpiredDate, @FileUrl, @Now);

DECLARE @CertificateId BIGINT = (SELECT TOP(1) CertificateId FROM @NewCertificateId);
DECLARE @PrevHash VARBINARY(32) = (SELECT TOP(1) ThisHash FROM audit.AuditLogs WITH (UPDLOCK, HOLDLOCK) ORDER BY AuditId DESC);
DECLARE @Payload NVARCHAR(MAX) = CONCAT(N'CertificateCode=', @CertificateCode, N';CertificateId=', @CertificateId);
DECLARE @HashInput NVARCHAR(MAX) = CONCAT(ISNULL(CONVERT(NVARCHAR(64), @PrevHash, 2), N''), N'|CERTIFICATE|', @CertificateId, N'|INSERT|', @Actor, N'|', CONVERT(NVARCHAR(33), @Now, 126), N'|', @Payload);
DECLARE @ThisHash VARBINARY(32) = HASHBYTES('SHA2_256', @HashInput);

INSERT INTO audit.AuditLogs(EntityName, EntityId, ActionType, ActionAt, Actor, PayloadText, PrevHash, ThisHash)
VALUES(N'CERTIFICATE', CONVERT(NVARCHAR(100), @CertificateId), N'INSERT', @Now, @Actor, @Payload, @PrevHash, @ThisHash);

COMMIT TRAN;
SELECT @CertificateId AS CertificateId;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@CertificateCode", request.CertificateCode);
        command.Parameters.AddWithValue("@CertificateName", request.CertificateName);
        command.Parameters.AddWithValue("@IssuedBy", (object?)request.IssuedBy ?? DBNull.Value);
        command.Parameters.AddWithValue("@IssuedDate", (object?)request.IssuedDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@ExpiredDate", (object?)request.ExpiredDate?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value);
        command.Parameters.AddWithValue("@FileUrl", (object?)request.FileUrl ?? DBNull.Value);
        command.Parameters.AddWithValue("@Actor", request.Actor);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result == DBNull.Value)
        {
            throw new InvalidOperationException("Create certificate did not return a certificate id.");
        }

        return Convert.ToInt64(result);
    }

    public async Task AttachCertificateAsync(string batchCode, AttachCertificateRequest request, CancellationToken cancellationToken)
    {
        const string sql = @"
BEGIN TRAN;

DECLARE @BatchId UNIQUEIDENTIFIER = (
    SELECT BatchId
    FROM scm.Batches WITH (UPDLOCK, HOLDLOCK)
    WHERE BatchCode = @BatchCode
);

IF @BatchId IS NULL
BEGIN
    ROLLBACK TRAN;
    THROW 51004, 'BatchCode not found.', 1;
END

IF NOT EXISTS (SELECT 1 FROM scm.Certificates WHERE CertificateId = @CertificateId)
BEGIN
    ROLLBACK TRAN;
    THROW 51005, 'CertificateId not found.', 1;
END

DECLARE @Now DATETIME2(3) = CONVERT(datetime2(3), SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
DECLARE @OldCertificateId BIGINT = (
    SELECT TOP(1) CertificateId
    FROM scm.BatchCertificates WITH (UPDLOCK, HOLDLOCK)
    WHERE BatchId = @BatchId
);

IF @OldCertificateId IS NULL
BEGIN
    INSERT INTO scm.BatchCertificates(BatchId, CertificateId, AttachedAt, AttachedBy)
    VALUES(@BatchId, @CertificateId, @Now, @Actor);
END
ELSE
BEGIN
    UPDATE scm.BatchCertificates
    SET CertificateId = @CertificateId,
        AttachedAt = @Now,
        AttachedBy = @Actor
    WHERE BatchId = @BatchId;
END

DECLARE @PrevHash VARBINARY(32) = (SELECT TOP(1) ThisHash FROM audit.AuditLogs WITH (UPDLOCK, HOLDLOCK) ORDER BY AuditId DESC);
DECLARE @ActionType NVARCHAR(30) = CASE WHEN @OldCertificateId IS NULL THEN N'ATTACH_CERT' ELSE N'CHANGE_CERT' END;
DECLARE @Payload NVARCHAR(MAX) = CONCAT(N'BatchCode=', @BatchCode, N';OldCertificateId=', ISNULL(CONVERT(NVARCHAR(20), @OldCertificateId), N'NULL'), N';CertificateId=', @CertificateId);
DECLARE @HashInput NVARCHAR(MAX) = CONCAT(ISNULL(CONVERT(NVARCHAR(64), @PrevHash, 2), N''), N'|BATCH_CERT|', CONVERT(NVARCHAR(36), @BatchId), N'|', @ActionType, N'|', @Actor, N'|', CONVERT(NVARCHAR(33), @Now, 126), N'|', @Payload);
DECLARE @ThisHash VARBINARY(32) = HASHBYTES('SHA2_256', @HashInput);

INSERT INTO audit.AuditLogs(EntityName, EntityId, ActionType, ActionAt, Actor, PayloadText, PrevHash, ThisHash)
VALUES(N'BATCH_CERT', CONVERT(NVARCHAR(36), @BatchId), @ActionType, @Now, @Actor, @Payload, @PrevHash, @ThisHash);

COMMIT TRAN;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchCode", batchCode);
        command.Parameters.AddWithValue("@CertificateId", request.CertificateId);
        command.Parameters.AddWithValue("@Actor", request.Actor);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CertificateDto>> GetCertificatesByBatchCodeAsync(string batchCode, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT c.CertificateId, c.CertificateCode, c.CertificateName, c.IssuedBy, c.IssuedDate, c.ExpiredDate, c.FileUrl,
       bc.AttachedAt, bc.AttachedBy
FROM scm.Batches b
INNER JOIN scm.BatchCertificates bc ON bc.BatchId = b.BatchId
INNER JOIN scm.Certificates c ON c.CertificateId = bc.CertificateId
WHERE b.BatchCode = @BatchCode
ORDER BY bc.AttachedAt DESC;";

        var list = new List<CertificateDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchCode", batchCode);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new CertificateDto
            {
                CertificateId = reader.GetInt64(reader.GetOrdinal("CertificateId")),
                CertificateCode = reader.GetString(reader.GetOrdinal("CertificateCode")),
                CertificateName = reader.GetString(reader.GetOrdinal("CertificateName")),
                IssuedBy = reader.IsDBNull(reader.GetOrdinal("IssuedBy")) ? null : reader.GetString(reader.GetOrdinal("IssuedBy")),
                IssuedDate = reader.IsDBNull(reader.GetOrdinal("IssuedDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("IssuedDate"))),
                ExpiredDate = reader.IsDBNull(reader.GetOrdinal("ExpiredDate")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("ExpiredDate"))),
                FileUrl = reader.IsDBNull(reader.GetOrdinal("FileUrl")) ? null : reader.GetString(reader.GetOrdinal("FileUrl")),
                AttachedAt = reader.GetDateTime(reader.GetOrdinal("AttachedAt")),
                AttachedBy = reader.GetString(reader.GetOrdinal("AttachedBy"))
            });
        }

        return list;
    }

    private async Task<IReadOnlyList<TraceEventDto>> QueryTraceAsync(string sql, SqlParameter parameter, CancellationToken cancellationToken)
    {
        var list = new List<TraceEventDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new TraceEventDto
            {
                BatchCode = reader.GetString(reader.GetOrdinal("BatchCode")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                CurrentStatus = reader.GetString(reader.GetOrdinal("CurrentStatus")),
                QRToken = reader.GetString(reader.GetOrdinal("QRToken")),
                TraceUrl = PublicTraceUrlBuilder.Build(reader.GetString(reader.GetOrdinal("QRToken"))),
                EventNo = reader.GetInt32(reader.GetOrdinal("EventNo")),
                EventType = reader.GetString(reader.GetOrdinal("EventType")),
                EventTime = reader.GetDateTime(reader.GetOrdinal("EventTime")),
                FromPartnerName = reader.IsDBNull(reader.GetOrdinal("FromPartnerName")) ? null : reader.GetString(reader.GetOrdinal("FromPartnerName")),
                ToPartnerName = reader.IsDBNull(reader.GetOrdinal("ToPartnerName")) ? null : reader.GetString(reader.GetOrdinal("ToPartnerName")),
                LocationText = reader.IsDBNull(reader.GetOrdinal("LocationText")) ? null : reader.GetString(reader.GetOrdinal("LocationText")),
                NoteText = reader.IsDBNull(reader.GetOrdinal("NoteText")) ? null : reader.GetString(reader.GetOrdinal("NoteText"))
            });
        }

        return list;
    }
}
