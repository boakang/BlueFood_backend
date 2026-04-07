USE BlueFoodSCM;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Set this to the latest UTC-written timestamp before ApplyVietnamTime.sql was applied.
  Example: if you applied patch around 2026-04-07 10:55 local, then UTC data is before 2026-04-07 10:55.
*/
DECLARE @CutoffLocal DATETIME2(3) = '2026-04-07T10:55:00.000';

BEGIN TRAN;

-- Temporarily disable immutable triggers for one-time migration.
DISABLE TRIGGER scm.TR_BatchEvents_BlockUpdateDelete ON scm.BatchEvents;
DISABLE TRIGGER scm.TR_BatchCertificates_BlockUpdateDelete ON scm.BatchCertificates;

UPDATE scm.Partners
SET CreatedAt = DATEADD(HOUR, 7, CreatedAt)
WHERE CreatedAt < @CutoffLocal;

UPDATE scm.Batches
SET CreatedAt = DATEADD(HOUR, 7, CreatedAt)
WHERE CreatedAt < @CutoffLocal;

UPDATE scm.BatchEvents
SET EventTime = DATEADD(HOUR, 7, EventTime)
WHERE EventTime < @CutoffLocal;

UPDATE scm.Certificates
SET CreatedAt = DATEADD(HOUR, 7, CreatedAt)
WHERE CreatedAt < @CutoffLocal;

UPDATE scm.BatchCertificates
SET AttachedAt = DATEADD(HOUR, 7, AttachedAt)
WHERE AttachedAt < @CutoffLocal;

UPDATE scm.BatchQRCodes
SET CreatedAt = DATEADD(HOUR, 7, CreatedAt)
WHERE CreatedAt < @CutoffLocal;

-- Do not update audit.AuditLogs to preserve hash-chain integrity.

ENABLE TRIGGER scm.TR_BatchEvents_BlockUpdateDelete ON scm.BatchEvents;
ENABLE TRIGGER scm.TR_BatchCertificates_BlockUpdateDelete ON scm.BatchCertificates;

COMMIT TRAN;
GO

SELECT 'DONE' AS MigrationStatus;
GO
