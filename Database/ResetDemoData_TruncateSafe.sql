USE BlueFoodSCM;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    -- Safe to truncate because no other table has a foreign key that points to these tables.
    TRUNCATE TABLE audit.AuditLogs;
    TRUNCATE TABLE scm.BatchEvents;
    TRUNCATE TABLE scm.BatchCertificates;
    TRUNCATE TABLE scm.BatchQRCodes;

    -- Keep scm.Partners untouched so demo references remain valid.
    -- These tables cannot be truncated safely without dropping foreign keys,
    -- so use DELETE in dependency-safe order.
    DELETE FROM scm.Batches;
    DELETE FROM scm.Certificates;

    -- After DELETE, reseed identity for predictable demo IDs.
    DBCC CHECKIDENT ('scm.Certificates', RESEED, 0);

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRAN;

    THROW;
END CATCH;

GO

-- Verify results
SELECT 'audit.AuditLogs' AS TableName, COUNT(*) AS [RowCount] FROM audit.AuditLogs
UNION ALL
SELECT 'scm.BatchEvents' AS TableName, COUNT(*) AS [RowCount] FROM scm.BatchEvents
UNION ALL
SELECT 'scm.BatchCertificates' AS TableName, COUNT(*) AS [RowCount] FROM scm.BatchCertificates
UNION ALL
SELECT 'scm.BatchQRCodes' AS TableName, COUNT(*) AS [RowCount] FROM scm.BatchQRCodes
UNION ALL
SELECT 'scm.Partners' AS TableName, COUNT(*) AS [RowCount] FROM scm.Partners
UNION ALL
SELECT 'scm.Batches' AS TableName, COUNT(*) AS [RowCount] FROM scm.Batches
UNION ALL
SELECT 'scm.Certificates' AS TableName, COUNT(*) AS [RowCount] FROM scm.Certificates;
GO