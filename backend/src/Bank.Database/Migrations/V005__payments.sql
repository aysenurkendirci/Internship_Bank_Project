-- Payments: transfers between accounts held at this bank, recorded in a double-entry ledger.
--
--   * payments.Transfers      -> one row per transfer request, keyed by the client's idempotency key
--   * payments.LedgerEntries  -> exactly one debit and one credit per transfer, with the balance after each
--   * dbo.Transactions        -> still written, as the customer-facing account statement the dashboard reads
--
-- Balances are changed only inside the transaction that writes the ledger, so the ledger can always
-- explain every balance. The CHECK constraints below are the last line of defence if that ever breaks.

CREATE SCHEMA payments;
GO

-- New accounts take their number from here; the IBAN is built from it by the application.
CREATE SEQUENCE payments.AccountNumbers AS BIGINT START WITH 1000000 INCREMENT BY 1;

-- A balance can never go below zero, whatever writes it.
ALTER TABLE dbo.Accounts ADD CONSTRAINT CK_Accounts_Balance_NotNegative CHECK (Balance >= 0);

CREATE TABLE payments.Transfers
(
    TransferId       BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transfers PRIMARY KEY,
    UserId           BIGINT         NOT NULL CONSTRAINT FK_Transfers_Users REFERENCES dbo.Users (UserId),
    IdempotencyKey   VARCHAR(64)    NOT NULL,
    -- SHA-256 of the request, to tell a genuine retry from a different request reusing the same key.
    RequestHash      BINARY(32)     NOT NULL,
    SourceAccountId  BIGINT         NOT NULL CONSTRAINT FK_Transfers_Source REFERENCES dbo.Accounts (AccountId),
    TargetAccountId  BIGINT         NOT NULL CONSTRAINT FK_Transfers_Target REFERENCES dbo.Accounts (AccountId),
    Amount           DECIMAL(19,4)  NOT NULL,
    CurrencyCode     CHAR(3)        NOT NULL,
    Description      NVARCHAR(140)  NOT NULL,
    CreatedAt        DATETIME2(3)   NOT NULL CONSTRAINT DF_Transfers_CreatedAt DEFAULT SYSUTCDATETIME(),

    -- Also the index the idempotency lookup seeks on.
    CONSTRAINT UQ_Transfers_IdempotencyKey UNIQUE (UserId, IdempotencyKey),
    CONSTRAINT CK_Transfers_Amount CHECK (Amount > 0),
    CONSTRAINT CK_Transfers_DifferentAccounts CHECK (SourceAccountId <> TargetAccountId)
);

CREATE TABLE payments.LedgerEntries
(
    EntryId       BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LedgerEntries PRIMARY KEY,
    TransferId    BIGINT         NOT NULL CONSTRAINT FK_LedgerEntries_Transfers REFERENCES payments.Transfers (TransferId),
    AccountId     BIGINT         NOT NULL CONSTRAINT FK_LedgerEntries_Accounts REFERENCES dbo.Accounts (AccountId),
    Side          VARCHAR(6)     NOT NULL,
    Amount        DECIMAL(19,4)  NOT NULL,
    CurrencyCode  CHAR(3)        NOT NULL,
    BalanceAfter  DECIMAL(19,4)  NOT NULL,
    CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_LedgerEntries_CreatedAt DEFAULT SYSUTCDATETIME(),

    -- One debit and one credit per transfer, never two of the same side.
    CONSTRAINT UQ_LedgerEntries_TransferSide UNIQUE (TransferId, Side),
    CONSTRAINT CK_LedgerEntries_Side CHECK (Side IN ('Debit', 'Credit')),
    CONSTRAINT CK_LedgerEntries_Amount CHECK (Amount > 0),
    CONSTRAINT CK_LedgerEntries_BalanceAfter CHECK (BalanceAfter >= 0)
);

-- Serves an account's statement, newest first.
CREATE INDEX IX_LedgerEntries_Account ON payments.LedgerEntries (AccountId, EntryId DESC);
