USE BlueFoodSCM;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

DECLARE @vnNowExpr NVARCHAR(500) =
N'(CONVERT(datetime2(3), SYSUTCDATETIME() AT TIME ZONE ''UTC'' AT TIME ZONE ''SE Asia Standard Time''))';

DECLARE @targets TABLE
(
    SchemaName SYSNAME,
    TableName SYSNAME,
    ColumnName SYSNAME,
    NewConstraintName SYSNAME
);

INSERT INTO @targets(SchemaName, TableName, ColumnName, NewConstraintName)
VALUES
(N'scm',   N'Partners',          N'CreatedAt', N'DF_Partners_CreatedAt'),
(N'scm',   N'Batches',           N'CreatedAt', N'DF_Batches_CreatedAt'),
(N'scm',   N'BatchEvents',       N'EventTime', N'DF_BatchEvents_EventTime'),
(N'scm',   N'Certificates',      N'CreatedAt', N'DF_Certificates_CreatedAt'),
(N'scm',   N'BatchCertificates', N'AttachedAt',N'DF_BatchCertificates_AttachedAt'),
(N'scm',   N'BatchQRCodes',      N'CreatedAt', N'DF_BatchQRCodes_CreatedAt'),
(N'audit', N'AuditLogs',         N'ActionAt',  N'DF_AuditLogs_ActionAt');

DECLARE
    @schema SYSNAME,
    @table SYSNAME,
    @column SYSNAME,
    @newDf SYSNAME,
    @oldDf SYSNAME,
    @sql NVARCHAR(MAX);

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
SELECT SchemaName, TableName, ColumnName, NewConstraintName
FROM @targets;

OPEN cur;
FETCH NEXT FROM cur INTO @schema, @table, @column, @newDf;

WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @oldDf = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    JOIN sys.tables t
        ON t.object_id = c.object_id
    JOIN sys.schemas s
        ON s.schema_id = t.schema_id
    WHERE s.name = @schema
      AND t.name = @table
      AND c.name = @column;

    IF @oldDf IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE [' + @schema + N'].[' + @table + N'] DROP CONSTRAINT [' + @oldDf + N'];';
        EXEC sp_executesql @sql;
    END

    SET @sql =
        N'ALTER TABLE [' + @schema + N'].[' + @table + N'] ' +
        N'ADD CONSTRAINT [' + @newDf + N'] DEFAULT ' + @vnNowExpr + N' FOR [' + @column + N'];';
    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @schema, @table, @column, @newDf;
END

CLOSE cur;
DEALLOCATE cur;

COMMIT TRAN;
GO

CREATE OR ALTER PROCEDURE scm.usp_CreateBatch
    @BatchCode      NVARCHAR(40),
    @ProductName    NVARCHAR(200),
    @FarmPartnerId  INT = NULL,
    @ProductionDate DATE = NULL,
    @ExpiryDate     DATE = NULL,
    @Actor          NVARCHAR(100),
    @TraceBaseUrl   NVARCHAR(300) = N'http://localhost:5085/t/'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @BatchId UNIQUEIDENTIFIER = NEWID();
    DECLARE @Now DATETIME2(3) = CONVERT(datetime2(3), SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
    DECLARE @QRToken NVARCHAR(80) = LOWER(REPLACE(CONVERT(NVARCHAR(36), NEWID()), N'-', N''));
    DECLARE @TraceUrl NVARCHAR(500) = CONCAT(N'/t/', @QRToken);

    INSERT INTO scm.Batches(BatchId, BatchCode, ProductName, FarmPartnerId, CurrentStatus, ProductionDate, ExpiryDate, CreatedBy, CreatedAt)
    VALUES(@BatchId, @BatchCode, @ProductName, @FarmPartnerId, N'CREATED', @ProductionDate, @ExpiryDate, @Actor, @Now);

    INSERT INTO scm.BatchQRCodes(BatchId, QRToken, TraceUrl, CreatedAt)
    VALUES(@BatchId, @QRToken, @TraceUrl, @Now);

    INSERT INTO scm.BatchEvents(BatchId, EventNo, EventType, FromPartnerId, ToPartnerId, LocationText, NoteText, EventTime, CreatedBy)
    VALUES(@BatchId, 1, N'CREATED', NULL, @FarmPartnerId, NULL, N'Initial creation', @Now, @Actor);

    DECLARE @PrevHash VARBINARY(32) = (SELECT TOP(1) ThisHash FROM audit.AuditLogs WITH (UPDLOCK, HOLDLOCK) ORDER BY AuditId DESC);
    DECLARE @Payload NVARCHAR(MAX) = CONCAT(N'BatchCode=', @BatchCode, N';Product=', @ProductName, N';Status=CREATED');
    DECLARE @HashInput NVARCHAR(MAX) = CONCAT(ISNULL(CONVERT(NVARCHAR(64), @PrevHash, 2), N''), N'|BATCH|', CONVERT(NVARCHAR(36), @BatchId), N'|INSERT|', @Actor, N'|', CONVERT(NVARCHAR(33), @Now, 126), N'|', @Payload);
    DECLARE @ThisHash VARBINARY(32) = HASHBYTES('SHA2_256', @HashInput);

    INSERT INTO audit.AuditLogs(EntityName, EntityId, ActionType, ActionAt, Actor, PayloadText, PrevHash, ThisHash)
    VALUES(N'BATCH', CONVERT(NVARCHAR(36), @BatchId), N'INSERT', @Now, @Actor, @Payload, @PrevHash, @ThisHash);

    COMMIT TRAN;

    SELECT @BatchId AS BatchId, @BatchCode AS BatchCode, @QRToken AS QRToken, @TraceUrl AS TraceUrl;
END
GO

CREATE OR ALTER PROCEDURE scm.usp_AddBatchEvent
    @BatchCode      NVARCHAR(40),
    @EventType      NVARCHAR(30),
    @FromPartnerId  INT = NULL,
    @ToPartnerId    INT = NULL,
    @LocationText   NVARCHAR(200) = NULL,
    @NoteText       NVARCHAR(500) = NULL,
    @Actor          NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

    DECLARE @BatchId UNIQUEIDENTIFIER = (SELECT BatchId FROM scm.Batches WITH (UPDLOCK, HOLDLOCK) WHERE BatchCode = @BatchCode);
    IF @BatchId IS NULL
    BEGIN
        THROW 51003, 'BatchCode not found.', 1;
    END

    DECLARE @NextEventNo INT = (
        SELECT ISNULL(MAX(EventNo), 0) + 1
        FROM scm.BatchEvents WITH (UPDLOCK, HOLDLOCK)
        WHERE BatchId = @BatchId
    );

    DECLARE @Now DATETIME2(3) = CONVERT(datetime2(3), SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');

    INSERT INTO scm.BatchEvents(BatchId, EventNo, EventType, FromPartnerId, ToPartnerId, LocationText, NoteText, EventTime, CreatedBy)
    VALUES(@BatchId, @NextEventNo, @EventType, @FromPartnerId, @ToPartnerId, @LocationText, @NoteText, @Now, @Actor);

    UPDATE scm.Batches
    SET CurrentStatus = @EventType
    WHERE BatchId = @BatchId;

    DECLARE @PrevHash VARBINARY(32) = (SELECT TOP(1) ThisHash FROM audit.AuditLogs WITH (UPDLOCK, HOLDLOCK) ORDER BY AuditId DESC);
    DECLARE @Payload NVARCHAR(MAX) = CONCAT(N'BatchCode=', @BatchCode, N';Event=', @EventType, N';EventNo=', @NextEventNo);
    DECLARE @HashInput NVARCHAR(MAX) = CONCAT(ISNULL(CONVERT(NVARCHAR(64), @PrevHash, 2), N''), N'|BATCH_EVENT|', CONVERT(NVARCHAR(36), @BatchId), N'|STATUS_CHANGE|', @Actor, N'|', CONVERT(NVARCHAR(33), @Now, 126), N'|', @Payload);
    DECLARE @ThisHash VARBINARY(32) = HASHBYTES('SHA2_256', @HashInput);

    INSERT INTO audit.AuditLogs(EntityName, EntityId, ActionType, ActionAt, Actor, PayloadText, PrevHash, ThisHash)
    VALUES(N'BATCH_EVENT', CONVERT(NVARCHAR(36), @BatchId), N'STATUS_CHANGE', @Now, @Actor, @Payload, @PrevHash, @ThisHash);

    COMMIT TRAN;
END
GO

CREATE OR ALTER PROCEDURE scm.usp_GetPartners
    @PartnerType TINYINT = NULL,
    @OnlyActive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        p.PartnerId,
        p.PartnerType,
        p.PartnerCode,
        p.PartnerName,
        p.IsActive
    FROM scm.Partners p
    WHERE (@OnlyActive = 0 OR p.IsActive = 1)
      AND (@PartnerType IS NULL OR p.PartnerType = @PartnerType)
    ORDER BY p.PartnerType, p.PartnerName;
END
GO

CREATE OR ALTER PROCEDURE scm.usp_GetDashboardOverview
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalBatches INT = (SELECT COUNT(1) FROM scm.Batches);
    DECLARE @TotalTraceEvents INT = (SELECT COUNT(1) FROM scm.BatchEvents);
    DECLARE @TotalCertificatesAttached INT = (SELECT COUNT(1) FROM scm.BatchCertificates);

    SELECT
        @TotalBatches AS TotalBatches,
        @TotalTraceEvents AS TotalTraceEvents,
        @TotalCertificatesAttached AS TotalCertificatesAttached;

    SELECT
        be.EventType AS Label,
        COUNT(1) AS Value
    FROM scm.BatchEvents be
    GROUP BY be.EventType
    ORDER BY COUNT(1) DESC, be.EventType;

    ;WITH EventAgg AS
    (
        SELECT
            CONVERT(VARCHAR(10), CAST(be.EventTime AS DATE), 23) AS Label,
            COUNT(1) AS Value
        FROM scm.BatchEvents be
        GROUP BY CAST(be.EventTime AS DATE)
    )
    SELECT TOP (7)
        ea.Label,
        ea.Value
    FROM EventAgg ea
    ORDER BY ea.Label DESC;
END
GO

CREATE OR ALTER PROCEDURE scm.usp_GetBatchManagement
    @Keyword NVARCHAR(100) = NULL,
    @Take INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @Take IS NULL OR @Take <= 0
        SET @Take = 100;

    ;WITH EventAgg AS
    (
        SELECT
            be.BatchId,
            COUNT(1) AS EventCount,
            MAX(be.EventTime) AS LastEventTime
        FROM scm.BatchEvents be
        GROUP BY be.BatchId
    ),
    CertAgg AS
    (
        SELECT
            bc.BatchId,
            COUNT(1) AS CertificateCount,
            MAX(c.CertificateName) AS CertificateName
        FROM scm.BatchCertificates bc
        LEFT JOIN scm.Certificates c ON c.CertificateId = bc.CertificateId
        GROUP BY bc.BatchId
    )
    SELECT TOP (@Take)
        b.BatchId,
        b.BatchCode,
        b.ProductName,
        b.CurrentStatus,
        b.CreatedBy,
        b.CreatedAt,
        p.PartnerName AS FarmPartnerName,
        ISNULL(ea.EventCount, 0) AS EventCount,
        ea.LastEventTime,
        ISNULL(ca.CertificateCount, 0) AS CertificateCount,
        ca.CertificateName
    FROM scm.Batches b
    LEFT JOIN scm.Partners p ON p.PartnerId = b.FarmPartnerId
    LEFT JOIN EventAgg ea ON ea.BatchId = b.BatchId
    LEFT JOIN CertAgg ca ON ca.BatchId = b.BatchId
    WHERE @Keyword IS NULL
       OR b.BatchCode LIKE N'%' + @Keyword + N'%'
       OR b.ProductName LIKE N'%' + @Keyword + N'%'
       OR b.CurrentStatus LIKE N'%' + @Keyword + N'%'
    ORDER BY b.CreatedAt DESC;
END
GO

CREATE OR ALTER PROCEDURE scm.usp_GetCertificateManagement
    @Keyword NVARCHAR(100) = NULL,
    @Take INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    IF @Take IS NULL OR @Take <= 0
        SET @Take = 100;

    ;WITH CertAgg AS
    (
        SELECT
            bc.CertificateId,
            COUNT(1) AS AttachedBatchCount,
            MAX(bc.AttachedAt) AS LastAttachedAt
        FROM scm.BatchCertificates bc
        GROUP BY bc.CertificateId
    )
    SELECT TOP (@Take)
        c.CertificateId,
        c.CertificateCode,
        c.CertificateName,
        c.IssuedBy,
        c.IssuedDate,
        c.ExpiredDate,
        c.FileUrl,
        c.CreatedAt,
        ISNULL(ca.AttachedBatchCount, 0) AS AttachedBatchCount,
        ca.LastAttachedAt
    FROM scm.Certificates c
    LEFT JOIN CertAgg ca ON ca.CertificateId = c.CertificateId
    WHERE @Keyword IS NULL
       OR c.CertificateCode LIKE N'%' + @Keyword + N'%'
       OR c.CertificateName LIKE N'%' + @Keyword + N'%'
       OR ISNULL(c.IssuedBy, N'') LIKE N'%' + @Keyword + N'%'
    ORDER BY c.CreatedAt DESC;
END
GO

IF OBJECT_ID(N'scm.TR_BatchCertificates_BlockUpdateDelete', N'TR') IS NOT NULL
    DROP TRIGGER scm.TR_BatchCertificates_BlockUpdateDelete;
GO

;WITH Dedup AS
(
    SELECT
        bc.BatchCertificateId,
        ROW_NUMBER() OVER (PARTITION BY bc.BatchId ORDER BY bc.AttachedAt DESC, bc.BatchCertificateId DESC) AS RowNo
    FROM scm.BatchCertificates bc
)
DELETE bc
FROM scm.BatchCertificates bc
INNER JOIN Dedup d ON d.BatchCertificateId = bc.BatchCertificateId
WHERE d.RowNo > 1;
GO

IF EXISTS (
    SELECT 1
    FROM sys.key_constraints kc
    WHERE kc.parent_object_id = OBJECT_ID(N'scm.BatchCertificates')
      AND kc.name = N'UQ_BatchCertificates'
)
BEGIN
    ALTER TABLE scm.BatchCertificates DROP CONSTRAINT UQ_BatchCertificates;
END
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'scm.BatchCertificates')
      AND i.name = N'UQ_BatchCertificates'
)
BEGIN
    DROP INDEX UQ_BatchCertificates ON scm.BatchCertificates;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.object_id = OBJECT_ID(N'scm.BatchCertificates')
      AND i.name = N'UQ_BatchCertificates_BatchId'
)
BEGIN
    CREATE UNIQUE INDEX UQ_BatchCertificates_BatchId
        ON scm.BatchCertificates(BatchId);
END
GO

CREATE OR ALTER TRIGGER scm.TR_BatchCertificates_BlockUpdateDelete
ON scm.BatchCertificates
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 52020, 'DELETE on scm.BatchCertificates is not allowed.', 1;
END
GO

CREATE OR ALTER PROCEDURE scm.usp_GetBatchesByCertificateId
    @CertificateId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        b.BatchId,
        b.BatchCode,
        b.ProductName,
        b.CurrentStatus,
        bc.AttachedAt,
        bc.AttachedBy
    FROM scm.BatchCertificates bc
    INNER JOIN scm.Batches b ON b.BatchId = bc.BatchId
    WHERE bc.CertificateId = @CertificateId
    ORDER BY bc.AttachedAt DESC;
END
GO

SELECT 'DONE' AS ApplyVietnamTimeStatus;
GO
