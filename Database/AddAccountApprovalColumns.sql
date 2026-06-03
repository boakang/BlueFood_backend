SET NOCOUNT ON;
SET XACT_ABORT ON;
USE BlueFoodSCM;

BEGIN TRY
    BEGIN TRAN;

    IF COL_LENGTH('scm.Users', 'Role') IS NULL
    BEGIN
        ALTER TABLE scm.Users ADD Role nvarchar(50) NULL;
    END

    IF COL_LENGTH('scm.Users', 'Status') IS NULL
    BEGIN
        ALTER TABLE scm.Users ADD Status nvarchar(20) NOT NULL CONSTRAINT DF_Users_Status DEFAULT('Active');
    END

    IF COL_LENGTH('scm.Users', 'ActivationToken') IS NULL
    BEGIN
        ALTER TABLE scm.Users ADD ActivationToken nvarchar(100) NULL;
    END

    IF COL_LENGTH('scm.Users', 'CreatedAt') IS NULL
    BEGIN
        ALTER TABLE scm.Users ADD CreatedAt datetime2(3) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME();
    END

        EXEC(N'UPDATE scm.Users
            SET Role = ISNULL(NULLIF(Role, ''''), ''User'')
            WHERE Role IS NULL OR LTRIM(RTRIM(Role)) = '''';');

        EXEC(N'UPDATE scm.Users
            SET Status = ISNULL(NULLIF(Status, ''''), ''Active'')
            WHERE Status IS NULL OR LTRIM(RTRIM(Status)) = '''';');

    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    THROW;
END CATCH;