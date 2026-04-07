USE BlueFoodSCM;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM scm.Partners WHERE PartnerCode = N'FARM001')
BEGIN
    INSERT INTO scm.Partners(PartnerType, PartnerCode, PartnerName)
    VALUES
    (1, N'FARM001', N'Nong trai A'),
    (2, N'TRANS001', N'Don vi van chuyen X'),
    (4, N'STORE001', N'Cua hang S');
END
GO

CREATE OR ALTER PROCEDURE scm.usp_SeedDemoData
    @BatchCount INT = 10000,
    @Actor NVARCHAR(100) = N'BAKHANG\Administrator',
    @TraceBaseUrl NVARCHAR(300) = N'http://localhost:5085/t/'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @FarmId INT = (SELECT TOP(1) PartnerId FROM scm.Partners WHERE PartnerCode = N'FARM001');
    DECLARE @TransId INT = (SELECT TOP(1) PartnerId FROM scm.Partners WHERE PartnerCode = N'TRANS001');
    DECLARE @StoreId INT = (SELECT TOP(1) PartnerId FROM scm.Partners WHERE PartnerCode = N'STORE001');

    IF @FarmId IS NULL OR @TransId IS NULL OR @StoreId IS NULL
    BEGIN
        THROW 51010, 'Demo partners are missing. Insert FARM001, TRANS001, STORE001 first.', 1;
    END

    DECLARE @i INT = 1;

    WHILE @i <= @BatchCount
    BEGIN
        DECLARE @BatchCode NVARCHAR(40) = CONCAT(N'BF-SEED-', RIGHT(CONCAT(N'00000', CONVERT(NVARCHAR(10), @i)), 5));
        DECLARE @ProductName NVARCHAR(200) = CASE (@i % 5)
            WHEN 0 THEN N'Xoai Cat Chu'
            WHEN 1 THEN N'Thanh Long'
            WHEN 2 THEN N'Cam Cao Phong'
            WHEN 3 THEN N'Dua Hau'
            ELSE N'Rau sach tong hop'
        END;
        DECLARE @ProductionDate DATE = DATEADD(DAY, -(@i % 30), CAST(GETDATE() AS DATE));
        DECLARE @ExpiryDate DATE = DATEADD(DAY, 14 + (@i % 15), @ProductionDate);

        EXEC scm.usp_CreateBatch
            @BatchCode = @BatchCode,
            @ProductName = @ProductName,
            @FarmPartnerId = @FarmId,
            @ProductionDate = @ProductionDate,
            @ExpiryDate = @ExpiryDate,
            @Actor = @Actor,
            @TraceBaseUrl = @TraceBaseUrl;

        IF @i % 2 = 0
        BEGIN
            EXEC scm.usp_AddBatchEvent
                @BatchCode = @BatchCode,
                @EventType = N'SHIPPED',
                @FromPartnerId = @FarmId,
                @ToPartnerId = @TransId,
                @LocationText = N'Dong Thap',
                @NoteText = N'Seeded shipment event',
                @Actor = @Actor;

            EXEC scm.usp_AddBatchEvent
                @BatchCode = @BatchCode,
                @EventType = N'RECEIVED',
                @FromPartnerId = @TransId,
                @ToPartnerId = @StoreId,
                @LocationText = N'HCM',
                @NoteText = N'Seeded receiving event',
                @Actor = @Actor;
        END

        SET @i += 1;
    END
END
GO

CREATE OR ALTER PROCEDURE scm.usp_BenchmarkTraceByBatchCode
    @BatchCode NVARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Start DATETIME2(3) = SYSUTCDATETIME();

    SELECT
        BatchCode,
        ProductName,
        CurrentStatus,
        QRToken,
        TraceUrl,
        EventNo,
        EventType,
        EventTime,
        FromPartnerName,
        ToPartnerName,
        LocationText,
        NoteText
    FROM scm.vw_BatchTrace
    WHERE BatchCode = @BatchCode
    ORDER BY EventNo;

    SELECT DATEDIFF(MILLISECOND, @Start, SYSUTCDATETIME()) AS ElapsedMs;
END
GO

-- Usage examples:
-- EXEC scm.usp_SeedDemoData @BatchCount = 10000;
-- EXEC scm.usp_BenchmarkTraceByBatchCode @BatchCode = N'BF-SEED-00001';