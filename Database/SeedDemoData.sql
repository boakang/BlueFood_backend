SET NOCOUNT ON;
SET XACT_ABORT ON;
USE BlueFoodSCM;

DECLARE @TraceBaseUrl nvarchar(300) = N'http://192.168.2.7:5085';
DECLARE @Creator nvarchar(50) = N'khang';
DECLARE @SeedStartDate date = '2025-05-29';

BEGIN TRY
    BEGIN TRAN;

    ;WITH N AS (
        SELECT TOP (199) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    )
    INSERT scm.Partners (PartnerType, PartnerCode, PartnerName, IsActive, CreatedAt)
    SELECT 1,
           CONCAT('FARM-SIM-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           CONCAT('Nong trai seeding ', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           1,
           DATEADD(day, -n, SYSDATETIME())
    FROM N
    WHERE NOT EXISTS (
        SELECT 1
        FROM scm.Partners p
        WHERE p.PartnerCode = CONCAT('FARM-SIM-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3))
    );

    ;WITH N AS (
        SELECT TOP (19) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    )
    INSERT scm.Partners (PartnerType, PartnerCode, PartnerName, IsActive, CreatedAt)
    SELECT 2,
           CONCAT('TRANS-SIM-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           CONCAT('Don vi van chuyen seeding ', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           1,
           DATEADD(day, -n, SYSDATETIME())
    FROM N
    WHERE NOT EXISTS (
        SELECT 1
        FROM scm.Partners p
        WHERE p.PartnerCode = CONCAT('TRANS-SIM-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3))
    );

    ;WITH N AS (
        SELECT TOP (49) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    )
    INSERT scm.Partners (PartnerType, PartnerCode, PartnerName, IsActive, CreatedAt)
    SELECT 4,
           CONCAT('STORE-SIM-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           CONCAT('Cua hang seeding ', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           1,
           DATEADD(day, -n, SYSDATETIME())
    FROM N
    WHERE NOT EXISTS (
        SELECT 1
        FROM scm.Partners p
        WHERE p.PartnerCode = CONCAT('STORE-SIM-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3))
    );

    ;WITH N AS (
        SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
    )
    INSERT scm.Certificates (CertificateCode, CertificateName, IssuedBy, IssuedDate, ExpiredDate, FileUrl, CreatedAt)
    SELECT CONCAT('CERT-SEED-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3)),
           CASE ((n - 1) % 5)
             WHEN 0 THEN N'VietGAP'
             WHEN 1 THEN N'GlobalGAP'
             WHEN 2 THEN N'HACCP'
             WHEN 3 THEN N'ISO 22000'
             ELSE N'Organic'
           END,
           CASE ((n - 1) % 5)
             WHEN 0 THEN N'Bo NNPTNT'
             WHEN 1 THEN N'GlobalGAP Org'
             WHEN 2 THEN N'HACCP Vietnam'
             WHEN 3 THEN N'ISO Cert'
             ELSE N'Organic Alliance'
           END,
           DATEADD(day, -((n - 1) % 300), CAST('2025-01-01' AS date)),
           DATEADD(day, 365, DATEADD(day, -((n - 1) % 300), CAST('2025-01-01' AS date))),
           CONCAT('https://bluefood.example/certificates/CERT-SEED-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3), '.pdf'),
           DATEADD(day, -n, SYSDATETIME())
    FROM N
    WHERE NOT EXISTS (
        SELECT 1
        FROM scm.Certificates c
        WHERE c.CertificateCode = CONCAT('CERT-SEED-', RIGHT(CONCAT('000', CAST(n AS varchar(3))), 3))
    );

    DECLARE @FarmPartners TABLE (RowNum int IDENTITY(1,1), PartnerId int NOT NULL);
    INSERT @FarmPartners (PartnerId)
    SELECT PartnerId
    FROM scm.Partners
    WHERE PartnerType = 1;

    DECLARE @TransportPartners TABLE (RowNum int IDENTITY(1,1), PartnerId int NOT NULL);
    INSERT @TransportPartners (PartnerId)
    SELECT PartnerId
    FROM scm.Partners
    WHERE PartnerType = 2;

    DECLARE @StorePartners TABLE (RowNum int IDENTITY(1,1), PartnerId int NOT NULL);
    INSERT @StorePartners (PartnerId)
    SELECT PartnerId
    FROM scm.Partners
    WHERE PartnerType = 4;

    DECLARE @FarmCount int = (SELECT COUNT(*) FROM @FarmPartners);
    DECLARE @TransportCount int = (SELECT COUNT(*) FROM @TransportPartners);
    DECLARE @StoreCount int = (SELECT COUNT(*) FROM @StorePartners);

    DECLARE @BatchSeed TABLE (
        SeedNo int NOT NULL PRIMARY KEY,
        BatchId uniqueidentifier NOT NULL,
        BatchCode nvarchar(40) NOT NULL,
        ProductName nvarchar(200) NOT NULL,
        FarmPartnerId int NULL,
        TransportPartnerId int NULL,
        StorePartnerId int NULL,
        CurrentStatus nvarchar(30) NOT NULL,
        ProductionDate date NULL,
        ExpiryDate date NULL,
        CreatedAt datetime2(3) NOT NULL,
        CreatedBy nvarchar(50) NULL,
        QRToken nvarchar(80) NOT NULL,
        TraceUrl nvarchar(500) NOT NULL
    );

    ;WITH N AS (
        SELECT TOP (10000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
        FROM sys.all_objects a CROSS JOIN sys.all_objects b
        CROSS JOIN sys.all_objects c
    )
    INSERT @BatchSeed (
        SeedNo, BatchId, BatchCode, ProductName, FarmPartnerId, TransportPartnerId, StorePartnerId,
        CurrentStatus, ProductionDate, ExpiryDate, CreatedAt, CreatedBy, QRToken, TraceUrl
    )
    SELECT n,
           CONVERT(uniqueidentifier, STUFF(STUFF(STUFF(STUFF(LEFT(CONVERT(varchar(32), HASHBYTES('MD5', CONCAT('BF-SIM-', RIGHT(CONCAT('00000', CAST(n AS varchar(5))), 5))), 2), 32), 9, 0, '-'), 14, 0, '-'), 19, 0, '-'), 24, 0, '-')),
           CONCAT('BF-SIM-', RIGHT(CONCAT('00000', CAST(n AS varchar(5))), 5)),
           CASE (n % 10)
             WHEN 0 THEN N'Xoai cat chu'
             WHEN 1 THEN N'Dua hau'
             WHEN 2 THEN N'Sua tuoi'
             WHEN 3 THEN N'Rau xanh tong hop'
             WHEN 4 THEN N'Tom nuoi'
             WHEN 5 THEN N'Ca tra'
             WHEN 6 THEN N'Chuoi sach'
             WHEN 7 THEN N'Dua luoi'
             WHEN 8 THEN N'Ca chua'
             ELSE N'Cam canh'
           END,
           fp.PartnerId,
           tp.PartnerId,
           sp.PartnerId,
           CASE
             WHEN n % 3 = 0 THEN N'RECEIVED'
             WHEN n % 2 = 0 THEN N'SHIPPED'
             ELSE N'CREATED'
           END,
           DATEADD(day, -((n % 21) + 7), CAST(DATEADD(day, (n - 1) % 365, CAST(@SeedStartDate AS datetime2(3))) AS date)),
           DATEADD(day, 90 + (n % 60), DATEADD(day, -((n % 21) + 7), CAST(DATEADD(day, (n - 1) % 365, CAST(@SeedStartDate AS datetime2(3))) AS date))),
           DATEADD(minute, n % 1440, DATEADD(day, (n - 1) % 365, CAST(@SeedStartDate AS datetime2(3)))),
           @Creator,
           CONVERT(varchar(80), HASHBYTES('SHA2_256', CONCAT('BF-SIM-', RIGHT(CONCAT('00000', CAST(n AS varchar(5))), 5))), 2),
           CONCAT(@TraceBaseUrl, '/t/', CONVERT(varchar(80), HASHBYTES('SHA2_256', CONCAT('BF-SIM-', RIGHT(CONCAT('00000', CAST(n AS varchar(5))), 5))), 2))
    FROM N
    JOIN @FarmPartners fp ON fp.RowNum = ((n - 1) % @FarmCount) + 1
    JOIN @TransportPartners tp ON tp.RowNum = ((n - 1) % @TransportCount) + 1
    JOIN @StorePartners sp ON sp.RowNum = ((n - 1) % @StoreCount) + 1;

    INSERT scm.Batches (BatchId, BatchCode, ProductName, FarmPartnerId, CurrentStatus, ProductionDate, ExpiryDate, CreatedBy, CreatedAt)
    SELECT s.BatchId, s.BatchCode, s.ProductName, s.FarmPartnerId, s.CurrentStatus, s.ProductionDate, s.ExpiryDate, s.CreatedBy, s.CreatedAt
    FROM @BatchSeed s
    WHERE NOT EXISTS (SELECT 1 FROM scm.Batches b WHERE b.BatchCode = s.BatchCode);

    INSERT scm.BatchQRCodes (BatchId, QRToken, TraceUrl, CreatedAt)
    SELECT s.BatchId, s.QRToken, s.TraceUrl, s.CreatedAt
    FROM @BatchSeed s
    WHERE NOT EXISTS (SELECT 1 FROM scm.BatchQRCodes q WHERE q.BatchId = s.BatchId);

    INSERT scm.BatchEvents (BatchId, EventNo, EventType, FromPartnerId, ToPartnerId, LocationText, NoteText, EventTime, CreatedBy)
    SELECT s.BatchId,
           v.EventNo,
           v.EventType,
           v.FromPartnerId,
           v.ToPartnerId,
           v.LocationText,
           v.NoteText,
           v.EventTime,
           @Creator
    FROM @BatchSeed s
    CROSS APPLY (
        VALUES
            (1, N'CREATED', s.FarmPartnerId, s.TransportPartnerId, CONCAT(N'Farm ', s.FarmPartnerId), CONCAT(N'Batch created for ', s.BatchCode), DATEADD(minute, 10, s.CreatedAt)),
            (2, N'SHIPPED', s.TransportPartnerId, s.StorePartnerId, CONCAT(N'Logistics ', s.TransportPartnerId), CONCAT(N'Batch shipped for ', s.BatchCode), DATEADD(hour, 18, s.CreatedAt)),
            (3, N'RECEIVED', s.TransportPartnerId, s.StorePartnerId, CONCAT(N'Store ', s.StorePartnerId), CONCAT(N'Batch received for ', s.BatchCode), DATEADD(day, 3, s.CreatedAt))
    ) v(EventNo, EventType, FromPartnerId, ToPartnerId, LocationText, NoteText, EventTime)
    WHERE (
        v.EventNo = 1
        OR (v.EventNo = 2 AND s.CurrentStatus IN (N'SHIPPED', N'RECEIVED'))
        OR (v.EventNo = 3 AND s.CurrentStatus = N'RECEIVED')
    )
    AND NOT EXISTS (
        SELECT 1
        FROM scm.BatchEvents e
        WHERE e.BatchId = s.BatchId
          AND e.EventNo = v.EventNo
    );

    DECLARE @CertLookup TABLE (RowNum int IDENTITY(1,1), CertificateId bigint NOT NULL);
    INSERT @CertLookup (CertificateId)
    SELECT x.CertificateId
    FROM (
        SELECT CertificateId, ROW_NUMBER() OVER (ORDER BY CertificateId) AS rn
        FROM scm.Certificates
        WHERE CertificateCode LIKE 'CERT-SEED-%'
    ) x;

    DECLARE @CertCount int = (SELECT COUNT(*) FROM @CertLookup);

    INSERT scm.BatchCertificates (BatchId, CertificateId, AttachedAt, AttachedBy)
    SELECT s.BatchId,
           c.CertificateId,
           DATEADD(hour, 1, s.CreatedAt),
           @Creator
    FROM @BatchSeed s
    JOIN @CertLookup c ON c.RowNum = ((s.SeedNo - 1) % @CertCount) + 1
    WHERE s.SeedNo % 5 = 0
      AND NOT EXISTS (
          SELECT 1
          FROM scm.BatchCertificates bc
          WHERE bc.BatchId = s.BatchId
            AND bc.CertificateId = c.CertificateId
      );

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;