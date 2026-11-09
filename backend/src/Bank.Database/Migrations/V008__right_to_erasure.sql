-- Right to erasure (KVKK article 11; docs/adr/0005).
--
-- The data splits by who produced it:
--   * personal finance data the customer entered themselves (finance.*) has no retention duty: it is deleted,
--     together with its change history, which a plain DELETE cannot reach because the temporal tables keep it
--   * the banking core (accounts, ledger, statements) must be kept for the legal retention period: it stays,
--     but the person behind it is removed from dbo.Users
--
-- compliance.ErasedIdentities keeps the identity number apart from everything else, only for as long as the
-- retention rules require it. Nothing but this procedure writes to it, and it should be readable only by the
-- compliance role.

CREATE SCHEMA compliance;
GO

CREATE TABLE compliance.ErasedIdentities
(
    UserId    BIGINT        NOT NULL CONSTRAINT PK_ErasedIdentities PRIMARY KEY CONSTRAINT FK_ErasedIdentities_Users REFERENCES dbo.Users (UserId),
    TcNo      CHAR(11)      NOT NULL,
    ErasedAt  DATETIME2(3)  NOT NULL
);
GO

-- EXECUTE AS OWNER lets the application run this one operation without holding the ALTER TABLE permission it
-- needs: the API's login only gets EXECUTE on the procedure. Switching versioning off takes a schema lock on the
-- three tables for the length of the transaction, which is acceptable for a rare operation and is why it
-- is done in one short statement sequence.
CREATE PROCEDURE compliance.EraseUser
    @UserId BIGINT,
    @Now    DATETIME2(3)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UserId)
        THROW 50001, 'User does not exist.', 1;

    -- Erasing twice is harmless.
    IF EXISTS (SELECT 1 FROM compliance.ErasedIdentities WHERE UserId = @UserId)
        RETURN;

    -- Money must be moved out first; closing an account that still holds some would make it disappear from view.
    IF EXISTS (SELECT 1 FROM dbo.Accounts WHERE UserId = @UserId AND Balance <> 0)
        THROW 50002, 'Accounts still hold a balance.', 1;

    BEGIN TRANSACTION;

    ALTER TABLE finance.CreditCards SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE finance.RecurringCashFlows SET (SYSTEM_VERSIONING = OFF);
    ALTER TABLE finance.InstallmentPlans SET (SYSTEM_VERSIONING = OFF);

    -- History first, then the rows that reference cards, then the cards themselves. The history deletes are
    -- dynamic SQL on purpose: while a table is system-versioned SQL Server refuses to even compile a DELETE
    -- against its history table, and the tables are only unversioned while this procedure runs.
    DECLARE @deleteHistory NVARCHAR(MAX) = N'
        DELETE FROM finance.InstallmentPlansHistory WHERE UserId = @UserId;
        DELETE FROM finance.RecurringCashFlowsHistory WHERE UserId = @UserId;
        DELETE FROM finance.CreditCardsHistory WHERE UserId = @UserId;';
    EXEC sp_executesql @deleteHistory, N'@UserId BIGINT', @UserId = @UserId;

    DELETE FROM finance.InstallmentPlans WHERE UserId = @UserId;
    DELETE FROM finance.RecurringCashFlows WHERE UserId = @UserId;
    DELETE FROM finance.CreditCards WHERE UserId = @UserId;
    DELETE FROM finance.Transactions WHERE UserId = @UserId;
    DELETE FROM finance.ImportBatches WHERE UserId = @UserId;

    ALTER TABLE finance.CreditCards
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = finance.CreditCardsHistory, DATA_CONSISTENCY_CHECK = OFF));
    ALTER TABLE finance.RecurringCashFlows
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = finance.RecurringCashFlowsHistory, DATA_CONSISTENCY_CHECK = OFF));
    ALTER TABLE finance.InstallmentPlans
        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = finance.InstallmentPlansHistory, DATA_CONSISTENCY_CHECK = OFF));

    -- Every open session ends. (One statement, so the self-reference between replaced tokens is not a problem.)
    DELETE FROM dbo.RefreshTokens WHERE UserId = @UserId;

    UPDATE dbo.Accounts SET Status = 'Closed' WHERE UserId = @UserId;
    UPDATE dbo.Cards SET Status = 'Cancelled' WHERE UserId = @UserId;

    INSERT INTO compliance.ErasedIdentities (UserId, TcNo, ErasedAt)
    SELECT UserId, TcNo, @Now FROM dbo.Users WHERE UserId = @UserId;

    -- The row stays because accounts and the ledger point at it. No field is left that identifies the person,
    -- and the password hash is not a valid hash, so nothing can sign in as this user.
    UPDATE dbo.Users
    SET TcNo         = CONCAT('E', RIGHT(CONCAT('0000000000', UserId), 10)),
        FirstName    = N'Silinmiş',
        LastName     = N'Kullanıcı',
        Email        = CONCAT('erased-', UserId, '@erased.invalid'),
        Phone        = '0',
        PasswordHash = '!',
        LastLoginAt  = NULL
    WHERE UserId = @UserId;

    COMMIT TRANSACTION;
END
